#!/usr/bin/env node
'use strict';

/**
 * Version Validation Script
 * Validates that all project files have consistent version numbers
 */

const fs = require('fs');
const path = require('path');

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
      // Skip inaccessible directories
    }
  }
  
  walk(directory);
  return results;
}

function validateVersions() {
  console.log('🔍 SQL Server Samples Version Validation');
  console.log('=========================================');
  
  // Load expected versions from config
  const config = JSON.parse(fs.readFileSync('version.json', 'utf8'));
  const expectedVersion = config.version;
  const expectedAssemblyVersion = config.projects.csharp.assemblyVersion;
  
  console.log(`📦 Expected version: ${expectedVersion}`);
  console.log(`🏗️  Expected assembly version: ${expectedAssemblyVersion}`);
  console.log('');
  
  let totalChecked = 0;
  let errors = 0;
  
  // Check package.json files
  console.log('📄 Checking package.json files...');
  const packageFiles = findFiles('.', ['package.json']);
  packageFiles.forEach(file => {
    try {
      const pkg = JSON.parse(fs.readFileSync(file, 'utf8'));
      if (pkg.version) {
        totalChecked++;
        if (pkg.version !== expectedVersion) {
          console.error(`❌ ${file}: expected ${expectedVersion}, found ${pkg.version}`);
          errors++;
        } else {
          console.log(`✅ ${file}`);
        }
      }
    } catch (error) {
      console.warn(`⚠️  Could not read ${file}: ${error.message}`);
    }
  });
  
  console.log('');
  
  // Check AssemblyInfo.cs files
  console.log('🏗️  Checking AssemblyInfo.cs files...');
  const assemblyFiles = findFiles('.', ['AssemblyInfo.cs']);
  assemblyFiles.forEach(file => {
    try {
      const content = fs.readFileSync(file, 'utf8');
      
      // Check AssemblyVersion
      const assemblyVersionMatch = content.match(/\[assembly:\s*AssemblyVersion\s*\(\s*"([^"]+)"\s*\)\]/);
      if (assemblyVersionMatch) {
        totalChecked++;
        const foundVersion = assemblyVersionMatch[1];
        if (foundVersion !== expectedAssemblyVersion) {
          console.error(`❌ ${file}: AssemblyVersion expected ${expectedAssemblyVersion}, found ${foundVersion}`);
          errors++;
        } else {
          console.log(`✅ ${file} (AssemblyVersion)`);
        }
      }
      
      // Check AssemblyFileVersion
      const fileVersionMatch = content.match(/\[assembly:\s*AssemblyFileVersion\s*\(\s*"([^"]+)"\s*\)\]/);
      if (fileVersionMatch) {
        totalChecked++;
        const foundVersion = fileVersionMatch[1];
        if (foundVersion !== expectedAssemblyVersion) {
          console.error(`❌ ${file}: AssemblyFileVersion expected ${expectedAssemblyVersion}, found ${foundVersion}`);
          errors++;
        } else {
          console.log(`✅ ${file} (AssemblyFileVersion)`);
        }
      }
    } catch (error) {
      console.warn(`⚠️  Could not read ${file}: ${error.message}`);
    }
  });
  
  console.log('');
  
  // Check composer.json
  console.log('📦 Checking composer.json...');
  if (fs.existsSync('composer.json')) {
    try {
      const composer = JSON.parse(fs.readFileSync('composer.json', 'utf8'));
      if (composer.version) {
        totalChecked++;
        if (composer.version !== expectedVersion) {
          console.error(`❌ composer.json: expected ${expectedVersion}, found ${composer.version}`);
          errors++;
        } else {
          console.log(`✅ composer.json`);
        }
      }
    } catch (error) {
      console.warn(`⚠️  Could not read composer.json: ${error.message}`);
    }
  }
  
  console.log('');
  console.log('📊 Validation Summary');
  console.log('====================');
  console.log(`Total files checked: ${totalChecked}`);
  console.log(`Errors found: ${errors}`);
  console.log(`Success rate: ${totalChecked > 0 ? ((totalChecked - errors) / totalChecked * 100).toFixed(1) : 0}%`);
  
  if (errors === 0) {
    console.log('');
    console.log('✅ All version numbers are consistent!');
    return true;
  } else {
    console.log('');
    console.error('❌ Version inconsistencies found. Run version update to fix:');
    console.error('   node update-versions.js patch');
    return false;
  }
}

// Export for programmatic use
module.exports = { validateVersions };

// Run if called directly
if (require.main === module) {
  const success = validateVersions();
  process.exit(success ? 0 : 1);
}