﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;

namespace RapidXamlToolkit.Confguration
{
    public class RxtSettings
    {
        private const string AppSettingsFileName = "appsettings.json";

        public RxtSettings()
        {
            var rawJson = File.ReadAllText(AppSettingsFileName);

            if (!string.IsNullOrWhiteSpace(rawJson))
            {
                try
                {
                    JObject obj = JObject.Parse(rawJson);

                    foreach (var property in obj.Properties())
                    {
                        switch (property.Name)
                        {
                            case "TelemetryKey":
                                this.TelemetryKey = property.Value.ToString();
                                break;
                            case "ExtendedOutputEnabledByDefault":
                                this.ExtendedOutputEnabledByDefault = bool.Parse(property.Value.ToString());
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch (Exception exc)
                {
                    Debug.WriteLine(exc);
                }
            }
        }

        public string TelemetryKey { get; private set; } = "DEFAULT-VALUE";

        public bool ExtendedOutputEnabledByDefault { get; private set; } = true;
    }
}
