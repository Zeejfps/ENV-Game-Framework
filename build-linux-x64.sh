#!/bin/sh

echo "Building image..."
docker build -t linux-x64-builder \
    --platform=linux/amd64 \
    --build-arg PROJECT_PATH=./NodeGraphApp/NodeGraphApp.csproj \
    -f ./build-linux-x64.dockerfile .

echo "Creating container..."
docker create --name build-artifacts linux-x64-builder

echo "Copying artifacts..."
docker run --rm --name build-artifacts linux-x64-builder cp -r /publish /publish




