#!/bin/sh
set -e

project_path="./NodeGraphApp/NodeGraphApp.csproj"
output_path="./builds/NodeGraphApp/"

echo "Building image..."
docker build -t linux-x64-builder \
    --build-arg \
        PROJECT_PATH=$project_path \
    -f ./build_linux-x64.dockerfile .

echo "Copying artifacts..."
container_name="build-artifacts"
mkdir -p $output_path
docker run --rm -d --name $container_name linux-x64-builder sleep infinity
docker cp $container_name:/linux-x64/ $output_path
docker stop $container_name





