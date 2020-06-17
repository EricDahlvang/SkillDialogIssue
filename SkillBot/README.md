# SimpleBotToBot Echo Skill

Bot Framework v4 skills echo sample.

This bot has been created using [Bot Framework](https://dev.botframework.com), it shows how to create a simple skill bot that echos messages back to the calling bot.

## Prerequisites

- [.NET Core SDK](https://dotnet.microsoft.com/download) version 2.1

  ```bash
  # determine dotnet version
  dotnet --version
  ```

## Key concepts in this sample

The solution includes a parent bot (`EchoBot`) skill and shows how a skill can respond and send messages to a calling bot.
  - An [AllowedCallersClaimsValidator](Authentication/AllowedCallersClaimsValidator.cs) class that is used to authenticate that responses sent to it are coming from the configured bots only.  If AllowedCallers in appsettings.json is empty, any RootBot can call the skill.
  - A [Startup](SimpleRootBot/Startup.cs) class that shows how to register the different components for dependency injection
  - A [sample skill manifest](wwwroot/manifest/echoskillbot-manifest-1.0.json) that describes what the skill can do

## To try this sample

- Clone the repository

    ```bash
    git clone https://github.com/microsoft/botbuilder-samples.git
    ```

- Create a bot registration in the azure portal for the `EchoSkillBot` and update [appsettings.json](appsettings.json) with the `MicrosoftAppId` and `MicrosoftAppPassword` of the new bot registration
- Separately, create a bot registration in the azure portal for the `RootBot` and update [appsettings.json](appsettings.json) with the `MicrosoftAppId` and `SkillEndpoint` of the new bot registration.  Note: If calling external skills from a locally hosted Root Bot, use the ngrok url for the SkillEndpoint
- Update the `BotFrameworkSkills` section in [appsettings.json](SimpleRootBot/appsettings.json) with the app ID for the skill you created in the previous step
- (Optionally) Add the `SimpleRootBot` `MicrosoftAppId` to the `AllowedCallers` list in [appsettings.json](appsettings.json) 

## Testing the bot using Bot Framework Emulator

[Bot Framework Emulator](https://github.com/microsoft/botframework-emulator) is a desktop application that allows bot developers to test and debug their bots on localhost or running remotely through a tunnel.

- Install the Bot Framework Emulator version 4.7.0 or greater from [here](https://github.com/Microsoft/BotFramework-Emulator/releases)

### Connect to the bot using Bot Framework Emulator

- Launch Bot Framework Emulator
- File -> Open Bot
- Enter a Bot URL of `http://localhost:39783/api/messages`, the `MicrosoftAppId` and `MicrosoftAppPassword` for the `EchoSkillBot`

## Deploy the bots to Azure

To learn more about deploying a bot to Azure, see [Deploy your bot to Azure](https://aka.ms/azuredeployment) for a complete list of deployment instructions.
