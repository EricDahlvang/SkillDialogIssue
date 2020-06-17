# SimpleBotToBot Root Bot

Bot Framework v4 skills echo sample.

This bot has been created using [Bot Framework](https://dev.botframework.com), it shows how to create a simple skill consumer (RootBot) that sends message activities to configured skills.  The code for this bot is from [80.skills-simple-bot-to-bot](https://github.com/microsoft/BotBuilder-Samples/tree/master/samples/csharp_dotnetcore/80.skills-simple-bot-to-bot)  

## Prerequisites

- [.NET Core SDK](https://dotnet.microsoft.com/download) version 2.1

  ```bash
  # determine dotnet version
  dotnet --version
  ```

## Key concepts in this sample

The project includes a parent bot (`RootBot`) and shows how to post activities to configured skill bots and returns skill responses to the user. 

- `RootBot`: this project shows how to consume skills and includes:
  - A [RootBot](Bots/RootBot.cs) provides a HeroCard with buttons enabling calling configured skills and keeps the conversation active until the user says "end" or "stop". [RootBot](Bots/RootBot.cs) handles the `EndOfConversation` activity received from the skill to terminate the conversation
  - A simple [SkillConversationIdFactory](SkillConversationIdFactory.cs) based on an in memory `ConcurrentDictionary` It creates and maintains conversation IDs used to interact with a skill
  - A [SkillsConfiguration](SkillsConfiguration.cs) class that can load skill definitions from `appsettings`
  - A [SkillController](Controllers/SkillController.cs) which handles skill responses
  - An [AllowedSkillsClaimsValidator](Authentication/AllowedSkillsClaimsValidator.cs) class that is used to authenticate responses sent to the bot are coming from the configured skills
  - A [Startup](Startup.cs) class that shows how to register the different skill components for dependency injection

## To try this sample

- Clone the repository

    ```bash
    git clone https://github.com/microsoft/botbuilder-samples.git
    ```

- Create a bot registration in the azure portal for the `RootBot` and update [appsettings.json](appsettings.json) with the `MicrosoftAppId` and `MicrosoftAppPassword` of the new bot registration
- Update the `BotFrameworkSkills` section in [appsettings.json](appsettings.json) with a custom id, the AppId (MicrosoftAppId for the skill) and SkillEndpoint
- (Optionally) Add the `RootBot`'s `MicrosoftAppId` to the `AllowedCallers` list in of any restricted Skills the bot will be calling 


## Testing the bot using Bot Framework Emulator

[Bot Framework Emulator](https://github.com/microsoft/botframework-emulator) is a desktop application that allows bot developers to test and debug their bots on localhost or running remotely through a tunnel.

- Install the Bot Framework Emulator version 4.7.0 or greater from [here](https://github.com/Microsoft/BotFramework-Emulator/releases)

### Connect to the bot using Bot Framework Emulator

- Launch Bot Framework Emulator
- File -> Open Bot
- Enter a Bot URL of `http://localhost:3978/api/messages`, the `MicrosoftAppId` and `MicrosoftAppPassword` for the `SimpleRootBot`

## Deploy the bots to Azure

To learn more about deploying a bot to Azure, see [Deploy your bot to Azure](https://aka.ms/azuredeployment) for a complete list of deployment instructions.
