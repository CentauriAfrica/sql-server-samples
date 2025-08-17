#!/bin/bash

# setup-azurite.sh - Setup and start Azurite for local testing

set -e

echo "🔧 Setting up Azurite for Entity Framework tests..."

# Check if Node.js is installed
if ! command -v npm &> /dev/null; then
    echo "❌ Node.js/npm not found. Please install Node.js first."
    exit 1
fi

# Check if azurite is installed globally
if ! command -v azurite &> /dev/null; then
    echo "📦 Installing Azurite globally..."
    npm install -g azurite
else
    echo "✅ Azurite is already installed"
fi

# Create azurite data directory
mkdir -p ./azurite

# Check if azurite is already running
if curl -s http://localhost:10000 > /dev/null 2>&1; then
    echo "✅ Azurite is already running"
else
    echo "🚀 Starting Azurite..."
    
    # Start azurite in background
    azurite --silent --location ./azurite --debug ./azurite/debug.log &
    AZURITE_PID=$!
    
    # Wait for azurite to start
    echo "⏳ Waiting for Azurite to start..."
    for i in {1..30}; do
        if curl -s http://localhost:10000 > /dev/null 2>&1; then
            echo "✅ Azurite started successfully!"
            echo "📝 Azurite PID: $AZURITE_PID"
            echo "📝 Data directory: ./azurite"
            echo "📝 Blob endpoint: http://127.0.0.1:10000/devstoreaccount1"
            echo "📝 Queue endpoint: http://127.0.0.1:10001/devstoreaccount1"
            echo "📝 Table endpoint: http://127.0.0.1:10002/devstoreaccount1"
            echo ""
            echo "🧪 You can now run your Entity Framework tests!"
            echo "🛑 To stop Azurite later, run: kill $AZURITE_PID"
            break
        fi
        sleep 1
    done
    
    if ! curl -s http://localhost:10000 > /dev/null 2>&1; then
        echo "❌ Failed to start Azurite"
        exit 1
    fi
fi

echo ""
echo "🔗 Connection string for tests: UseDevelopmentStorage=true"
echo "📋 Default account name: devstoreaccount1"
echo "🔑 Default account key: Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="