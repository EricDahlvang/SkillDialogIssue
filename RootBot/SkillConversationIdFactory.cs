﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio SkillRootBot v4.7.1

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace SkillDialogIssueRootBot
{
    /// <summary>
    /// A <see cref="SkillConversationIdFactory"/> that uses an in memory <see cref="ConcurrentDictionary{TKey,TValue}"/>
    /// to store and retrieve <see cref="SkillConversationReference"/> instances.
    /// </summary>
    public class SkillConversationIdFactory : SkillConversationIdFactoryBase
    {
        private readonly ConcurrentDictionary<string, string> _conversationRefs = new ConcurrentDictionary<string, string>();

        public override Task<string> CreateSkillConversationIdAsync(SkillConversationIdFactoryOptions options, CancellationToken cancellationToken)
        {
            var skillConversationReference = new SkillConversationReference
            {
                ConversationReference = options.Activity.GetConversationReference(),
                OAuthScope = options.FromBotOAuthScope
            };
            var key = $"{options.FromBotId}-{options.BotFrameworkSkill.AppId}-{skillConversationReference.ConversationReference.Conversation.Id}-{skillConversationReference.ConversationReference.ChannelId}-skillconvo";
            _conversationRefs.GetOrAdd(key, JsonConvert.SerializeObject(skillConversationReference));
            return Task.FromResult(key);
        }

        public override Task<SkillConversationReference> GetSkillConversationReferenceAsync(string skillConversationId, CancellationToken cancellationToken)
        {
            var conversationReference = JsonConvert.DeserializeObject<SkillConversationReference>(_conversationRefs[skillConversationId]);
            return Task.FromResult(conversationReference);
        }

        public override Task DeleteConversationReferenceAsync(string skillConversationId, CancellationToken cancellationToken)
        {
            _conversationRefs.TryRemove(skillConversationId, out _);
            return Task.CompletedTask;
        }
    }
}
