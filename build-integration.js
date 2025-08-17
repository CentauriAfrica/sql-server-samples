#!/usr/bin/env node
'use strict';

/**
 * Build Integration Script for Version Management
 * Automatically increments versions based on build triggers
 */

const { updateVersions, loadVersionConfig } = require('./update-versions');
const { execSync } = require('child_process');

// Detect build environment and increment strategy
function detectBuildStrategy() {
  // GitHub Actions environment
  if (process.env.GITHUB_ACTIONS) {
    const event = process.env.GITHUB_EVENT_NAME;
    const ref = process.env.GITHUB_REF;
    
    console.log(`🏗️  GitHub Actions build detected: ${event} on ${ref}`);
    
    // Major version for releases
    if (event === 'release' || (ref && ref.startsWith('refs/tags/v'))) {
      return 'major';
    }
    
    // Minor version for main/master branch pushes
    if (event === 'push' && (ref === 'refs/heads/main' || ref === 'refs/heads/master')) {
      return 'minor';
    }
    
    // Patch version for all other pushes and PRs
    return 'patch';
  }
  
  // Azure DevOps environment
  if (process.env.AZURE_HTTP_USER_AGENT) {
    const reason = process.env.BUILD_REASON;
    const branch = process.env.BUILD_SOURCEBRANCHNAME;
    
    console.log(`🏗️  Azure DevOps build detected: ${reason} on ${branch}`);
    
    if (reason === 'Manual' && branch === 'release') {
      return 'major';
    }
    
    if (branch === 'main' || branch === 'master') {
      return 'minor';
    }
    
    return 'patch';
  }
  
  // Jenkins environment
  if (process.env.JENKINS_HOME) {
    const branch = process.env.GIT_BRANCH || process.env.BRANCH_NAME;
    
    console.log(`🏗️  Jenkins build detected on branch: ${branch}`);
    
    if (branch && branch.includes('release')) {
      return 'major';
    }
    
    if (branch === 'origin/main' || branch === 'origin/master' || branch === 'main' || branch === 'master') {
      return 'minor';
    }
    
    return 'patch';
  }
  
  // Default for unknown environments
  console.log('🏗️  Build environment not detected, using patch increment');
  return 'patch';
}

// Check if version should be updated based on changes
function shouldUpdateVersion() {
  try {
    // Skip version update if only documentation files changed
    const changedFiles = execSync('git diff --name-only HEAD~1 HEAD', { encoding: 'utf8' });
    const fileList = changedFiles.trim().split('\n').filter(f => f);
    
    // Check if only docs/readme files changed
    const onlyDocs = fileList.every(file => 
      file.toLowerCase().includes('readme') ||
      file.toLowerCase().includes('.md') ||
      file.toLowerCase().includes('doc') ||
      file.toLowerCase().includes('license') ||
      file.toLowerCase().includes('.txt')
    );
    
    if (onlyDocs && fileList.length > 0) {
      console.log('📝 Only documentation files changed, skipping version update');
      return false;
    }
    
    return true;
  } catch (error) {
    // If we can't determine changes, proceed with version update
    console.log('⚠️  Could not determine file changes, proceeding with version update');
    return true;
  }
}

// Main build integration function
function buildIntegration() {
  console.log('🚀 SQL Server Samples Build Integration');
  console.log('======================================');
  
  // Check if we should update version
  if (!shouldUpdateVersion()) {
    return { updated: false, reason: 'Only documentation files changed' };
  }
  
  // Detect strategy based on build environment
  const strategy = detectBuildStrategy();
  console.log(`📈 Version increment strategy: ${strategy}`);
  console.log('');
  
  try {
    // Update versions
    const result = updateVersions(strategy);
    
    // Create git tag for the new version (if in CI environment)
    if (process.env.CI && !process.env.GITHUB_ACTIONS) {
      try {
        execSync(`git tag -a v${result.newVersion} -m "Release version ${result.newVersion}"`, { stdio: 'inherit' });
        console.log(`🏷️  Created git tag: v${result.newVersion}`);
      } catch (error) {
        console.warn('⚠️  Could not create git tag:', error.message);
      }
    }
    
    return {
      updated: true,
      version: result.newVersion,
      buildNumber: result.buildNumber,
      strategy: strategy
    };
  } catch (error) {
    console.error('❌ Build integration failed:', error.message);
    throw error;
  }
}

// Export for programmatic use
module.exports = {
  buildIntegration,
  detectBuildStrategy,
  shouldUpdateVersion
};

// Run if called directly
if (require.main === module) {
  try {
    const result = buildIntegration();
    
    if (result.updated) {
      console.log('');
      console.log(`✅ Build integration completed successfully!`);
      console.log(`📦 New version: ${result.version} (Build ${result.buildNumber})`);
      console.log(`📈 Strategy: ${result.strategy}`);
    } else {
      console.log('');
      console.log('✅ Build integration completed - no version update needed');
      console.log(`📄 Reason: ${result.reason}`);
    }
  } catch (error) {
    console.error('❌ Build integration failed:', error.message);
    process.exit(1);
  }
}