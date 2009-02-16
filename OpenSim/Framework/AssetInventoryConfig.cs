/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;

namespace OpenSim.Framework
{
    /// <summary>
    /// AssetInventoryConfig -- For AssetInventory Server Configuration
    /// </summary>
    public class AssetInventoryConfig
    {
        private ConfigurationMember configMember;

        public const uint DefaultHttpPort = 8003;
        public uint HttpPort = DefaultHttpPort;

        public string AssetStorageProvider = "OpenSimAssetStorage";
        public string AssetDatabaseConnect = String.Empty;
        public string InventoryStorageProvider = "OpenSimInventoryStorage";
        public string InventoryDatabaseConnect = String.Empty;

        public string AuthenticationProvider = "NullAuthentication";
        public string AuthorizationProvider = "AuthorizeAll";
        public string MetricsProvider = "NullMetrics";
        public string Frontends = "OpenSimAssetFrontend,OpenSimInventoryFrontend";

        public AssetInventoryConfig(string description, string filename)
        {
            configMember = new ConfigurationMember(filename, description, loadConfigurationOptions, handleIncomingConfiguration, true);
            configMember.performConfigurationRetrieve();
        }

        public void loadConfigurationOptions()
        {
            configMember.addConfigurationOption("listen_port",
                                                ConfigurationOption.ConfigurationTypes.TYPE_UINT32,
                                                "HTTP listener port",
                                                DefaultHttpPort.ToString(),
                                                false);

            configMember.addConfigurationOption("asset_storage_provider",
                                                ConfigurationOption.ConfigurationTypes.TYPE_STRING,
                                                "Asset storage provider",
                                                AssetStorageProvider,
                                                false);
            configMember.addConfigurationOption("asset_database_connect",
                                                ConfigurationOption.ConfigurationTypes.TYPE_STRING,
                                                "Asset database connection string",
                                                AssetDatabaseConnect,
                                                false);
            configMember.addConfigurationOption("inventory_storage_provider",
                                                ConfigurationOption.ConfigurationTypes.TYPE_STRING,
                                                "Inventory storage provider",
                                                InventoryStorageProvider,
                                                false);
            configMember.addConfigurationOption("inventory_database_connect",
                                                ConfigurationOption.ConfigurationTypes.TYPE_STRING,
                                                "Inventory database connection string",
                                                InventoryDatabaseConnect,
                                                false);

            configMember.addConfigurationOption("authentication_provider",
                                                ConfigurationOption.ConfigurationTypes.TYPE_STRING,
                                                "Authentication provider",
                                                AuthenticationProvider,
                                                false);
            configMember.addConfigurationOption("authorization_provider",
                                                ConfigurationOption.ConfigurationTypes.TYPE_STRING,
                                                "Authentication provider",
                                                AuthorizationProvider,
                                                false);
            configMember.addConfigurationOption("metrics_provider",
                                                ConfigurationOption.ConfigurationTypes.TYPE_STRING,
                                                "Metrics provider",
                                                MetricsProvider,
                                                false);
            configMember.addConfigurationOption("frontends",
                                                ConfigurationOption.ConfigurationTypes.TYPE_STRING,
                                                "Comma-separated list of frontends",
                                                Frontends,
                                                false);

        }

        public bool handleIncomingConfiguration(string configuration_key, object configuration_result)
        {
            switch (configuration_key)
            {
                case "listen_port":
                    HttpPort = (uint) configuration_result;
                    break;
                case "asset_storage_provider":
                    AssetStorageProvider = (string) configuration_result;
                    break;
                case "asset_database_connect":
                    AssetDatabaseConnect = (string) configuration_result;
                    break;
                case "inventory_storage_provider":
                    InventoryStorageProvider = (string) configuration_result;
                    break;
                case "inventory_database_connect":
                    InventoryDatabaseConnect = (string) configuration_result;
                    break;
                case "authentication_provider":
                    AuthenticationProvider = (string) configuration_result;
                    break;
                case "authorization_provider":
                    AuthorizationProvider = (string) configuration_result;
                    break;
                case "metrics_provider":
                    MetricsProvider = (string) configuration_result;
                    break;
                case "frontends":
                    Frontends = (string) configuration_result;
                    break;
            }

            return true;
        }
    }
}