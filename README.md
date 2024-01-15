# NovelAIBot

Image generation bot for Viva.

Sources copied from Kyaru.

Sources copied in their bare-minimum form to facilitate requests


# Contributing
Just make a pull request and notify Stillkill

# Pre-requisites
- .NET SDK v8

# Build
To best replicate the production build, use the following build settings     
`dotnet publish NovelAIBot.csproj -f net8.0 --self-contained --runtime linux-x64 -c release -p:PublishSingleFile=true`  
Note: Unused Code Trimming is not recommended, Discord.NET is not compatible with assembly trimming and will break. Don't worry, we won't be sore for the 50Mb difference in application size.
