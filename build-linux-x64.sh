#!/bin/sh

echo "Building image..."
docker build -t linux-x64-builder \
    --build-arg \
        PROJECT_PATH=./NodeGraphApp/NodeGraphApp.csproj \
    -f ./build-linux-x64.dockerfile .

echo "Copying artifacts..."
mkdir -p ./publish/NodeGraphApp/
docker run --rm -d --name build-artifacts linux-x64-builder sleep infinity
docker cp build-artifacts:/linux-x64/ ./builds/NodeGraphApp/
docker stop build-artifacts





