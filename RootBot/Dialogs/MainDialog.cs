﻿using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SkillDialogIssueRootBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private const string SkillActionMessage = "message";
        private const string SkillActionEnd = "ask skill to end";
        
        public static readonly string ActiveSkillPropertyName = $"{typeof(MainDialog).FullName}.ActiveSkillProperty";
        private readonly IStatePropertyAccessor<BotFrameworkSkill> _activeSkillProperty;
        private readonly string _selectedSkillKey = $"{typeof(MainDialog).FullName}.SelectedSkillKey";
        private readonly SkillsConfiguration _skillsConfig;

        public MainDialog(ConversationState conversationState, SkillConversationIdFactoryBase conversationIdFactory, SkillHttpClient skillClient, SkillsConfiguration skillsConfig, IConfiguration configuration)
            : base(nameof(MainDialog))
        {
            var botId = configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;
            if (string.IsNullOrWhiteSpace(botId))
            {
                throw new ArgumentException($"{MicrosoftAppCredentials.MicrosoftAppIdKey} is not in configuration");
            }

            _skillsConfig = skillsConfig ?? throw new ArgumentNullException(nameof(skillsConfig));

            if (skillClient == null)
            {
                throw new ArgumentNullException(nameof(skillClient));
            }

            if (conversationState == null)
            {
                throw new ArgumentNullException(nameof(conversationState));
            }

            // Use helper method to add SkillDialog instances for the configured skills.
            AddSkillDialogs(conversationState, conversationIdFactory, skillClient, skillsConfig, botId);

            // Add ChoicePrompt to render available skills.
            AddDialog(new ChoicePrompt("SkillPrompt"));

            // Add ChoicePrompt to render skill actions.
            AddDialog(new ChoicePrompt("SkillActionPrompt", SkillActionPromptValidator));

            // Add main waterfall dialog for this bot.
            var waterfallSteps = new WaterfallStep[]
            {
                SelectSkillStepAsync,
                SelectSkillActionStepAsync,
                CallSkillActionStepAsync,
                FinalStepAsync
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // Create state property to track the active skill.
            _activeSkillProperty = conversationState.CreateProperty<BotFrameworkSkill>(ActiveSkillPropertyName);

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            // This is an example on how to cancel a SkillDialog that is currently in progress from the parent bot.
            var activeSkill = await _activeSkillProperty.GetAsync(innerDc.Context, () => null, cancellationToken);
            var activity = innerDc.Context.Activity;
            if (activeSkill != null && activity.Type == ActivityTypes.Message && activity.Text.Equals("abort", StringComparison.CurrentCultureIgnoreCase))
            {
                // Cancel all dialogs when the user says abort.
                // The SkillDialog automatically sends an EndOfConversation message to the skill to let the
                // skill know that it needs to end its current dialogs, too.
                await innerDc.CancelAllDialogsAsync(cancellationToken);
                return await innerDc.ReplaceDialogAsync(InitialDialogId, "Canceled! \n\n What skill would you like to call?", cancellationToken);
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        // Render a prompt to select the skill to call.
        private async Task<DialogTurnResult> SelectSkillStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Create the PromptOptions from the skill configuration which contain the list of configured skills.
            var messageText = stepContext.Options?.ToString() ?? "What skill would you like to call?";
            var repromptMessageText = "That was not a valid choice, please select a valid skill.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = _skillsConfig.Skills.Select(skill => new Choice(skill.Value.Id)).ToList()
            };

            // Prompt the user to select a skill.
            return await stepContext.PromptAsync("SkillPrompt", options, cancellationToken);
        }

        // Render a prompt to select the action for the skill.
        private async Task<DialogTurnResult> SelectSkillActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the skill info based on the selected skill.
            var selectedSkillId = ((FoundChoice)stepContext.Result).Value;
            var selectedSkill = _skillsConfig.Skills.FirstOrDefault(s => s.Value.Id == selectedSkillId).Value;

            // Remember the skill selected by the user.
            stepContext.Values[_selectedSkillKey] = selectedSkill;

            // Create the PromptOptions with the actions supported by the selected skill.
            var messageText = $"Select an action # to send to **{selectedSkill.Id}** or just type in a message and it will be forwarded to the skill";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                Choices = GetSkillActions(selectedSkill)
            };

            // Prompt the user to select a skill action.
            return await stepContext.PromptAsync("SkillActionPrompt", options, cancellationToken);
        }

        // This validator defaults to Message if the user doesn't select an existing option.
        private Task<bool> SkillActionPromptValidator(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            if (!promptContext.Recognized.Succeeded)
            {
                // Assume the user wants to send a message if an item in the list is not selected.
                promptContext.Recognized.Value = new FoundChoice { Value = SkillActionMessage };
            }

            return Task.FromResult(true);
        }

        // Starts the SkillDialog based on the user's selections.
        private async Task<DialogTurnResult> CallSkillActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var selectedSkill = (BotFrameworkSkill)stepContext.Values[_selectedSkillKey];

            Activity skillActivity;
            switch (selectedSkill.Id)
            {
                case "DialogSkillBot":
                    skillActivity = CreateDialogSkillBotActivity(((FoundChoice)stepContext.Result).Value, stepContext.Context);
                    break;

                // We can add other case statements here if we support more than one skill.
                default:
                    throw new Exception($"Unknown target skill id: {selectedSkill.Id}.");
            }

            // Create the BeginSkillDialogOptions and assign the activity to send.
            var skillDialogArgs = new BeginSkillDialogOptions { Activity = skillActivity };

            // Save active skill in state.
            await _activeSkillProperty.SetAsync(stepContext.Context, selectedSkill, cancellationToken);

            // Start the skillDialog instance with the arguments. 
            return await stepContext.BeginDialogAsync(selectedSkill.Id, skillDialogArgs, cancellationToken);
        }

        // The SkillDialog has ended, render the results (if any) and restart MainDialog.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if(stepContext.Context.Activity.Type == ActivityTypes.EndOfConversation)
            {
                //NOTE: this means the Skill ended the conversation while the SkillDialog in the parent
                // was still processing
                throw new Exception("BUG!  Final step is EndOfConversation!  This means the Skill send EndOfConversation, while the SkillDialog was still processing a previous message.");
            }

            var activeSkill = await _activeSkillProperty.GetAsync(stepContext.Context, () => null, cancellationToken);

            // Check if the skill returned any results and display them.
            if (stepContext.Result != null)
            {
                var message = $"Skill \"{activeSkill.Id}\" invocation complete.";
                message += $" Result: {JsonConvert.SerializeObject(stepContext.Result)}";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(message, message, inputHint: InputHints.IgnoringInput), cancellationToken: cancellationToken);
            }

            // Clear the skill selected by the user.
            stepContext.Values[_selectedSkillKey] = null;

            // Clear active skill in state.
            await _activeSkillProperty.DeleteAsync(stepContext.Context, cancellationToken);

            // Restart the main dialog with a different message the second time around.
            return await stepContext.ReplaceDialogAsync(InitialDialogId, $"Done with \"{activeSkill.Id}\". \n\n What skill would you like to call?", cancellationToken);
        }

        // Helper method that creates and adds SkillDialog instances for the configured skills.
        private void AddSkillDialogs(ConversationState conversationState, SkillConversationIdFactoryBase conversationIdFactory, SkillHttpClient skillClient, SkillsConfiguration skillsConfig, string botId)
        {
            foreach (var skillInfo in _skillsConfig.Skills.Values)
            {
                // Create the dialog options.
                var skillDialogOptions = new SkillDialogOptions
                {
                    BotId = botId,
                    ConversationIdFactory = conversationIdFactory,
                    SkillClient = skillClient,
                    SkillHostEndpoint = skillsConfig.SkillHostEndpoint,
                    ConversationState = conversationState,
                    Skill = skillInfo
                };

                // Add a SkillDialog for the selected skill.
                AddDialog(new SkillDialog(skillDialogOptions, skillInfo.Id));
            }
        }

        // Helper method to create Choice elements for the actions supported by the skill.
        private IList<Choice> GetSkillActions(BotFrameworkSkill skill)
        {
            // Note: the bot would probably render this by reading the skill manifest.
            // We are just using hardcoded skill actions here for simplicity.

            var choices = new List<Choice>();
            switch (skill.Id)
            {
                case "DialogSkillBot":
                    choices.Add(new Choice(SkillActionEnd));
                    choices.Add(new Choice(SkillActionMessage));
                    break;
            }

            return choices;
        }

        // Helper method to create the activity to be sent to the DialogSkillBot using selected type and values.
        private Activity CreateDialogSkillBotActivity(string selectedOption, ITurnContext turnContext)
        {
            // Note: in a real bot, the dialogArgs will be created dynamically based on the conversation
            // and what each action requires; here we hardcode the values to make things simpler.

            Activity activity = null;
            // Just forward the message activity to the skill with whatever the user said. 
            if (selectedOption.Equals(SkillActionMessage, StringComparison.CurrentCultureIgnoreCase))
            {
                // Note message activities also support input parameters but we are not using them in this example.
                // Return a deep clone of the activity so we don't risk altering the original one 
                activity = ObjectPath.Clone(turnContext.Activity);
            }
            else if (selectedOption.Equals(SkillActionEnd, StringComparison.CurrentCultureIgnoreCase))
            {
                // Note message activities also support input parameters but we are not using them in this example.
                // Return a deep clone of the activity so we don't risk altering the original one 
                activity = ObjectPath.Clone(turnContext.Activity);
                activity.Text = "end";
            }

            if (activity == null)
            {
                throw new Exception($"Unable to create a skill activity for \"{selectedOption}\".");
            }

            // We are manually creating the activity to send to the skill; ensure we add the ChannelData and Properties 
            // from the original activity so the skill gets them.
            // Note: this is not necessary if we are just forwarding the current activity from context. 
            activity.ChannelData = turnContext.Activity.ChannelData;
            activity.Properties = turnContext.Activity.Properties;

            return activity;
        }
    }
}
