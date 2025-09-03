#!/bin/bash

dotnet publish -c Debug -r linux-x64 -p:PublishSingleFile=true --self-contained true 