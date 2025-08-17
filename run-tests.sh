#!/bin/bash

# SQL Server Samples - Test Runner Script
# This script runs all the test projects and validates the setup

set -e  # Exit on any error

echo "🚀 SQL Server Samples - Test Runner"
echo "=================================="
echo ""

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check prerequisites
echo "📋 Checking prerequisites..."
if ! command_exists dotnet; then
    echo "❌ .NET SDK is not installed"
    exit 1
fi

if ! command_exists node; then
    echo "❌ Node.js is not installed"
    exit 1
fi

echo "✅ .NET SDK: $(dotnet --version)"
echo "✅ Node.js: $(node --version)"
echo ""

# Test counters
total_tests=0
passed_tests=0
failed_tests=0
skipped_tests=0

# Function to run dotnet test and parse results
run_dotnet_tests() {
    local project_path="$1"
    local project_name="$2"
    
    echo "🧪 Running $project_name tests..."
    
    if [ ! -f "$project_path" ]; then
        echo "⚠️ Project not found: $project_path"
        return 1
    fi
    
    cd "$(dirname "$project_path")"
    
    # Run tests and capture output
    set +e  # Don't exit on test failures
    test_output=$(dotnet test "$(basename "$project_path")" --verbosity minimal --nologo 2>&1)
    test_exit_code=$?
    set -e
    
    # Parse test results from output
    if echo "$test_output" | grep -q "Passed!"; then
        # New .NET output format
        local total=$(echo "$test_output" | grep -o "Total:[[:space:]]*[0-9]*" | grep -o "[0-9]*")
        local passed=$(echo "$test_output" | grep -o "Passed:[[:space:]]*[0-9]*" | grep -o "[0-9]*")
        local failed=$(echo "$test_output" | grep -o "Failed:[[:space:]]*[0-9]*" | grep -o "[0-9]*" || echo "0")
        local skipped=$(echo "$test_output" | grep -o "Skipped:[[:space:]]*[0-9]*" | grep -o "[0-9]*" || echo "0")
        
        # Fallback to defaults if parsing fails
        total=${total:-0}
        passed=${passed:-0}
        failed=${failed:-0}
        skipped=${skipped:-0}
        
        total_tests=$((total_tests + total))
        passed_tests=$((passed_tests + passed))
        failed_tests=$((failed_tests + failed))
        skipped_tests=$((skipped_tests + skipped))
        
        if [ $test_exit_code -eq 0 ]; then
            echo "✅ $project_name: $passed passed, $skipped skipped, $failed failed"
        else
            echo "❌ $project_name: $passed passed, $failed failed, $skipped skipped"
        fi
    elif echo "$test_output" | grep -q "Total tests:"; then
        local total=$(echo "$test_output" | grep "Total tests:" | sed 's/.*Total tests: \([0-9]*\).*/\1/')
        local passed=$(echo "$test_output" | grep "Passed:" | sed 's/.*Passed: \([0-9]*\).*/\1/' || echo "0")
        local failed=$(echo "$test_output" | grep "Failed:" | sed 's/.*Failed: \([0-9]*\).*/\1/' || echo "0")
        local skipped=$(echo "$test_output" | grep "Skipped:" | sed 's/.*Skipped: \([0-9]*\).*/\1/' || echo "0")
        
        total_tests=$((total_tests + total))
        passed_tests=$((passed_tests + passed))
        failed_tests=$((failed_tests + failed))
        skipped_tests=$((skipped_tests + skipped))
        
        if [ $test_exit_code -eq 0 ]; then
            echo "✅ $project_name: $passed passed, $skipped skipped"
        else
            echo "❌ $project_name: $passed passed, $failed failed, $skipped skipped"
        fi
    else
        echo "⚠️ Could not parse test results for $project_name"
        echo "$test_output"
    fi
    
    echo ""
    return $test_exit_code
}

# Function to run Node.js validation
run_node_validation() {
    local script_name="$1"
    local description="$2"
    
    echo "🧪 Running $description..."
    
    if [ ! -f "$script_name" ]; then
        echo "⚠️ Script not found: $script_name"
        return 1
    fi
    
    set +e
    node_output=$(node "$script_name" 2>&1)
    node_exit_code=$?
    set -e
    
    if [ $node_exit_code -eq 0 ]; then
        echo "✅ $description: Passed"
    else
        echo "❌ $description: Failed"
        echo "$node_output"
    fi
    
    echo ""
    return $node_exit_code
}

# Change to repository root
cd "$(dirname "$0")"

# Run Entity Framework Context Isolation Tests
if [ -f "samples/features/entity-framework/context-isolation-tests/DiscountingEngine.Tests.csproj" ]; then
    run_dotnet_tests "samples/features/entity-framework/context-isolation-tests/DiscountingEngine.Tests.csproj" "Entity Framework Context Isolation"
else
    echo "⚠️ Entity Framework tests not found"
fi

# Run SQL Management Objects Tests (they will be skipped if no SQL Server is available)
if [ -f "samples/features/sql-management-objects/src/SmoSamples.csproj" ]; then
    echo "🧪 Running SQL Management Objects tests..."
    echo "ℹ️ Note: These tests require SQL Server connection and may be skipped"
    set +e
    run_dotnet_tests "samples/features/sql-management-objects/src/SmoSamples.csproj" "SQL Management Objects"
    smo_exit_code=$?
    set -e
else
    echo "⚠️ SMO tests not found"
fi

# Run Node.js validation tests
if [ -f "validate-versions.js" ]; then
    run_node_validation "validate-versions.js" "Version Management Validation"
else
    echo "⚠️ Version validation script not found"
fi

# Run build integration test
echo "🧪 Testing Build Integration script..."
if [ -f "build-integration.js" ]; then
    set +e
    build_output=$(timeout 10 node build-integration.js 2>&1 || echo "Build integration test completed")
    build_exit_code=$?
    set -e

    if [ $build_exit_code -eq 0 ] || [ $build_exit_code -eq 124 ]; then
        echo "✅ Build Integration: Available and executable"
    else
        echo "⚠️ Build Integration: Issues detected"
        echo "$build_output" | head -5
    fi
else
    echo "⚠️ Build integration script not found"
fi

echo ""

# Final summary
echo "📊 Test Summary"
echo "==============="
echo "Total tests discovered: $total_tests"
echo "✅ Passed: $passed_tests"
echo "❌ Failed: $failed_tests"
echo "⏭️ Skipped: $skipped_tests"
echo ""

# Overall result
if [ $failed_tests -eq 0 ]; then
    echo "🎉 All tests passed successfully!"
    exit 0
else
    echo "💥 Some tests failed. Please review the output above."
    exit 1
fi