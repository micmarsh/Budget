#!/bin/sh

cd Budget.CommandLine
dotnet publish
cp bin/Release/net10.0/linux-x64/publish/Budget.CommandLine ../budget
