#!/bin/sh

docker build -t linux-x64-builder \
    --progress=plain \
    --build-arg PROJECT_PATH=./NodeGraphApp/NodeGraphApp.csproj \
    -f ./build-linux-x64.dockerfile .
docker create --name build-artifacts linux-x64-builder
docker cp build-artifacts:/publish ./publish
docker rm build-artifacts



