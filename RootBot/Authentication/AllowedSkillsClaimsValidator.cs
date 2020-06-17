﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio SkillRootBot v4.7.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.Authentication;

namespace SkillDialogIssueRootBot.Authentication
{
    /// <summary>
    /// Sample claims validator that loads an allowed list from configuration if present
    /// and checks that responses are coming from configured skills.
    /// </summary>
    public class AllowedSkillsClaimsValidator : ClaimsValidator
    {
        private readonly List<string> _allowedSkills;

        public AllowedSkillsClaimsValidator(SkillsConfiguration skillsConfig)
        {
            if (skillsConfig == null)
            {
                throw new ArgumentNullException(nameof(skillsConfig));
            }

            // Load the appIds for the configured skills (we will only allow responses from skills we have configured).
            _allowedSkills = (from skill in skillsConfig.Skills.Values select skill.AppId).ToList();
        }

        public override Task ValidateClaimsAsync(IList<Claim> claims)
        {
            if (_allowedSkills.Count > 0 && SkillValidation.IsSkillClaim(claims))
            {
                // Check that the appId claim in the skill request is in the list of skills configured for this bot.
                var appId = JwtTokenValidation.GetAppIdFromClaims(claims);
                if (!_allowedSkills.Contains(appId))
                {
                    throw new UnauthorizedAccessException($"Received a request from an application with an appID of \"{appId}\". To enable requests from this skill, add the skill to your configuration file.");
                }
            }

            return Task.CompletedTask;
        }
    }
}