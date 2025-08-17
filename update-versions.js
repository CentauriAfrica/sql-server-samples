#!/usr/bin/env node
'use strict';

/**
 * Universal Version Management Script for SQL Server Samples
 * Automatically updates version numbers across all project types
 */

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

// Configuration
const VERSION_CONFIG_FILE = path.join(__dirname, 'version.json');
const DRY_RUN = process.argv.includes('--dry-run');
const VERBOSE = process.argv.includes('--verbose') || process.argv.includes('-v');

// Load version configuration
function loadVersionConfig() {
  try {
    const config = JSON.parse(fs.readFileSync(VERSION_CONFIG_FILE, 'utf8'));
    return config;
  } catch (error) {
    console.error('Failed to load version configuration:', error.message);
    process.exit(1);
  }
}

// Save version configuration
function saveVersionConfig(config) {
  if (DRY_RUN) {
    console.log('[DRY RUN] Would save version config:', JSON.stringify(config, null, 2));
    return;
  }
  
  try {
    fs.writeFileSync(VERSION_CONFIG_FILE, JSON.stringify(config, null, 2));
    if (VERBOSE) console.log('Version configuration saved successfully');
  } catch (error) {
    console.error('Failed to save version configuration:', error.message);
    process.exit(1);
  }
}

// Increment version based on strategy
function incrementVersion(currentVersion, strategy = 'patch') {
  const [major, minor, patch] = currentVersion.split('.').map(Number);
  
  switch (strategy) {
    case 'major':
      return `${major + 1}.0.0`;
    case 'minor':
      return `${major}.${minor + 1}.0`;
    case 'patch':
    default:
      return `${major}.${minor}.${patch + 1}`;
  }
}

// Find and process files recursively
function findFiles(directory, patterns, excludedDirs = []) {
  const results = [];
  const DEFAULT_EXCLUDED = ['.git', 'node_modules', 'bin', 'obj', 'dist', 'build', 'vendor', '.vs'];
  const excluded = new Set([...DEFAULT_EXCLUDED, ...excludedDirs]);
  
  function walk(dir) {
    if (excluded.has(path.basename(dir))) return;
    
    try {
      const items = fs.readdirSync(dir);
      items.forEach(item => {
        const fullPath = path.join(dir, item);
        const stat = fs.lstatSync(fullPath);
        
        if (stat.isDirectory()) {
          walk(fullPath);
        } else if (stat.isFile()) {
          const matches = patterns.some(pattern => {
            if (typeof pattern === 'string') {
              return item === pattern || fullPath.endsWith(pattern);
            } else if (pattern instanceof RegExp) {
              return pattern.test(item);
            }
            return false;
          });
          
          if (matches) {
            results.push(fullPath);
          }
        }
      });
    } catch (error) {
      if (VERBOSE) console.warn(`Warning: Could not read directory ${dir}:`, error.message);
    }
  }
  
  walk(directory);
  return results;
}

// Update C# AssemblyInfo.cs files
function updateAssemblyInfoFiles(newVersion, config) {
  const files = findFiles(process.cwd(), ['AssemblyInfo.cs']);
  const assemblyVersion = config.projects.csharp.assemblyVersion;
  const fileVersion = config.projects.csharp.fileVersion;
  
  files.forEach(file => {
    try {
      let content = fs.readFileSync(file, 'utf8');
      let updated = false;
      
      // Update AssemblyVersion
      const assemblyVersionRegex = /\[assembly:\s*AssemblyVersion\s*\(\s*"([^"]+)"\s*\)\]/g;
      if (assemblyVersionRegex.test(content)) {
        content = content.replace(assemblyVersionRegex, `[assembly: AssemblyVersion("${assemblyVersion}")]`);
        updated = true;
      }
      
      // Update AssemblyFileVersion
      const fileVersionRegex = /\[assembly:\s*AssemblyFileVersion\s*\(\s*"([^"]+)"\s*\)\]/g;
      if (fileVersionRegex.test(content)) {
        content = content.replace(fileVersionRegex, `[assembly: AssemblyFileVersion("${fileVersion}")]`);
        updated = true;
      }
      
      if (updated) {
        if (!DRY_RUN) {
          fs.writeFileSync(file, content);
        }
        console.log(`${DRY_RUN ? '[DRY RUN] Would update' : 'Updated'} ${path.relative(process.cwd(), file)}`);
      }
    } catch (error) {
      console.warn(`Warning: Could not update ${file}:`, error.message);
    }
  });
}

// Update C# project files (.csproj)
function updateCSharpProjectFiles(newVersion, config) {
  const files = findFiles(process.cwd(), [/\.csproj$/]);
  
  files.forEach(file => {
    try {
      let content = fs.readFileSync(file, 'utf8');
      let updated = false;
      
      // Update Version tags
      const versionRegex = /<Version>([^<]+)<\/Version>/g;
      if (versionRegex.test(content)) {
        content = content.replace(versionRegex, `<Version>${newVersion}</Version>`);
        updated = true;
      }
      
      // Update PackageVersion tags
      const packageVersionRegex = /<PackageVersion>([^<]+)<\/PackageVersion>/g;
      if (packageVersionRegex.test(content)) {
        content = content.replace(packageVersionRegex, `<PackageVersion>${newVersion}</PackageVersion>`);
        updated = true;
      }
      
      if (updated) {
        if (!DRY_RUN) {
          fs.writeFileSync(file, content);
        }
        console.log(`${DRY_RUN ? '[DRY RUN] Would update' : 'Updated'} ${path.relative(process.cwd(), file)}`);
      }
    } catch (error) {
      console.warn(`Warning: Could not update ${file}:`, error.message);
    }
  });
}

// Update Node.js package.json files
function updatePackageJsonFiles(newVersion) {
  const files = findFiles(process.cwd(), ['package.json']);
  
  files.forEach(file => {
    try {
      const packageData = JSON.parse(fs.readFileSync(file, 'utf8'));
      
      if (packageData.version) {
        packageData.version = newVersion;
        
        if (!DRY_RUN) {
          fs.writeFileSync(file, JSON.stringify(packageData, null, 2) + '\n');
        }
        console.log(`${DRY_RUN ? '[DRY RUN] Would update' : 'Updated'} ${path.relative(process.cwd(), file)}`);
      }
    } catch (error) {
      console.warn(`Warning: Could not update ${file}:`, error.message);
    }
  });
}

// Update JavaScript library versions using existing change-version scripts
function updateJavaScriptLibraries(config) {
  // Update Bootstrap
  const bootstrapDir = path.join(process.cwd(), 'samples/databases/wide-world-importers/wwi-app/wwwroot/lib/bootstrap');
  const bootstrapScript = path.join(bootstrapDir, 'grunt/change-version.js');
  
  if (fs.existsSync(bootstrapScript) && fs.existsSync(path.join(bootstrapDir, 'package.json'))) {
    try {
      const packageData = JSON.parse(fs.readFileSync(path.join(bootstrapDir, 'package.json'), 'utf8'));
      const currentVersion = packageData.version;
      const newVersion = config.projects.javascript.bootstrap;
      
      if (currentVersion !== newVersion) {
        if (!DRY_RUN) {
          execSync(`node "${bootstrapScript}" "${currentVersion}" "${newVersion}"`, { 
            cwd: bootstrapDir,
            stdio: VERBOSE ? 'inherit' : 'pipe'
          });
        }
        console.log(`${DRY_RUN ? '[DRY RUN] Would update' : 'Updated'} Bootstrap from ${currentVersion} to ${newVersion}`);
      }
    } catch (error) {
      console.warn('Warning: Could not update Bootstrap version:', error.message);
    }
  }
}

// Update version in Composer file
function updateComposerVersion(newVersion) {
  const composerFile = path.join(process.cwd(), 'composer.json');
  
  if (fs.existsSync(composerFile)) {
    try {
      const composerData = JSON.parse(fs.readFileSync(composerFile, 'utf8'));
      
      // Add version field if it doesn't exist
      if (!composerData.version) {
        composerData.version = newVersion;
        
        if (!DRY_RUN) {
          fs.writeFileSync(composerFile, JSON.stringify(composerData, null, 4) + '\n');
        }
        console.log(`${DRY_RUN ? '[DRY RUN] Would update' : 'Updated'} composer.json version to ${newVersion}`);
      }
    } catch (error) {
      console.warn('Warning: Could not update composer.json:', error.message);
    }
  }
}

// Main update function
function updateVersions(strategy = 'patch') {
  console.log('🔧 SQL Server Samples Version Manager');
  console.log('=====================================');
  
  if (DRY_RUN) {
    console.log('🚨 DRY RUN MODE - No files will be modified');
  }
  
  const config = loadVersionConfig();
  const currentVersion = config.version;
  const newVersion = incrementVersion(currentVersion, strategy);
  
  console.log(`📦 Current version: ${currentVersion}`);
  console.log(`📦 New version: ${newVersion}`);
  console.log('');
  
  // Update version configuration
  config.version = newVersion;
  config.buildNumber += 1;
  config.projects.csharp.assemblyVersion = `${newVersion}.${config.buildNumber}`;
  config.projects.csharp.fileVersion = `${newVersion}.${config.buildNumber}`;
  config.projects.nodejs.version = newVersion;
  config.lastUpdated = new Date().toISOString();
  config.buildHistory.push({
    version: newVersion,
    buildNumber: config.buildNumber,
    timestamp: config.lastUpdated,
    strategy: strategy
  });
  
  // Keep only last 10 builds in history
  if (config.buildHistory.length > 10) {
    config.buildHistory = config.buildHistory.slice(-10);
  }
  
  console.log('📁 Updating project files...');
  console.log('');
  
  // Update all file types
  updateAssemblyInfoFiles(newVersion, config);
  updateCSharpProjectFiles(newVersion, config);
  updatePackageJsonFiles(newVersion);
  updateJavaScriptLibraries(config);
  updateComposerVersion(newVersion);
  
  // Save updated configuration
  saveVersionConfig(config);
  
  console.log('');
  console.log(`✅ Version update completed! New version: ${newVersion} (Build ${config.buildNumber})`);
  
  return { newVersion, buildNumber: config.buildNumber };
}

// Command line interface
function main() {
  const args = process.argv.slice(2).filter(arg => !arg.startsWith('--'));
  const strategy = args[0] || 'patch';
  
  if (!['major', 'minor', 'patch'].includes(strategy)) {
    console.error('❌ Invalid strategy. Use: major, minor, or patch');
    console.error('Usage: node update-versions.js [major|minor|patch] [--dry-run] [--verbose]');
    process.exit(1);
  }
  
  try {
    updateVersions(strategy);
  } catch (error) {
    console.error('❌ Version update failed:', error.message);
    process.exit(1);
  }
}

// Export for programmatic use
module.exports = {
  updateVersions,
  loadVersionConfig,
  incrementVersion
};

// Run if called directly
if (require.main === module) {
  main();
}