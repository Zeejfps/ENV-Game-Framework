#!/bin/sh

echo "Building image..."
docker build -t linux-x64-builder \
    --platform=linux/amd64 \
    --build-arg PROJECT_PATH=./NodeGraphApp/NodeGraphApp.csproj \
    -f ./build-linux-x64.dockerfile .

echo "Copying artifacts..."
docker run --rm --name build-artifacts \
    --platform=linux/amd64 \
    linux-x64-builder cp -r /publish /publish




