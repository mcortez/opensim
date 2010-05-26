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
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;

using log4net;
using Mono.Addins;
using Nini.Config;

using OpenMetaverse;
using OpenMetaverse.StructuredData;

using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;

/*
using OpenSim.Region.CoreModules.Framework.EventQueue;
using Caps = OpenSim.Framework.Capabilities.Caps;
*/
using DirFindFlags = OpenMetaverse.DirectoryManager.DirFindFlags;



namespace OpenSim.Region.OptionalModules.SGSearch
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class SGSearch : ISharedRegionModule
    {
        /// <summary>
        /// ; To use this module, you must specify the following in your OpenSim.ini
        /// [Search]
        /// Enabled = true
        /// Module   = SGSearch
        /// DebugEnabled   = true
        /// 
        /// </summary>

        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private List<Scene> m_sceneList = new List<Scene>();


        // Configuration settings
        private bool m_moduleEnabled = false;
        private bool m_debugEnabled = true;

        private string m_simianGridServerURI = string.Empty;

        private ExpiringCache<string, OSDMap> m_memoryCache;
        private int m_cacheTimeout = 30;

        private IUserAccountService m_accountService = null;



        #region IRegionModuleBase Members

        public void Initialise(IConfigSource config)
        {
            IConfig searchConfig = config.Configs["Search"];

            if (searchConfig == null)
            {
                // Do not run this module by default.
                return;
            }
            else
            {
                m_moduleEnabled = searchConfig.GetBoolean("Enabled", false);
                if (!m_moduleEnabled)
                {
                    return;
                }

                if (searchConfig.GetString("Module", "SGSearch") != Name)
                {
                    m_moduleEnabled = false;

                    return;
                }

                m_log.InfoFormat("[SGSearch]: Initializing {0}", this.Name);

                m_simianGridServerURI = searchConfig.GetString("ServerURI", string.Empty);
                if ((m_simianGridServerURI == null) ||
                    (m_simianGridServerURI == string.Empty))
                {
                    m_log.ErrorFormat("[SGSearch] Please specify a valid Simian Server for ServerURI in OpenSim.ini, [Search]");
                    m_moduleEnabled = false;
                    return;
                }


                m_cacheTimeout = searchConfig.GetInt("CacheTimeout", 30);
                if (m_cacheTimeout == 0)
                {
                    m_log.WarnFormat("[SGSearch] Cache Disabled.");
                }
                else
                {
                    m_log.InfoFormat("[SGSearch] Cache Timeout set to {0}.", m_cacheTimeout);
                }

                m_debugEnabled = searchConfig.GetBoolean("DebugEnabled", true);

                m_memoryCache = new ExpiringCache<string, OSDMap>();
            }
        }

        public void AddRegion(Scene scene)
        {
            if (m_moduleEnabled)
            {
                if (m_accountService == null)
                {
                    m_accountService = scene.UserAccountService;
                }

            }
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_moduleEnabled)
                return;

            if (m_debugEnabled) m_log.DebugFormat("[SGSearch]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            

            lock (m_sceneList)
            {
                m_sceneList.Add(scene);
            }

            scene.EventManager.OnNewClient += OnNewClient;
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_moduleEnabled)
                return;

            if (m_debugEnabled) m_log.DebugFormat("[SGSearch]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            lock (m_sceneList)
            {
                m_sceneList.Remove(scene);
            }
        }

        public void Close()
        {
            if (!m_moduleEnabled)
                return;

            if (m_debugEnabled) m_log.DebugFormat("[SGSearch]: Shutting down {0} module.", this.Name);
        }

        public Type ReplaceableInterface 
        {
            get { return null; }
        }

        public string Name
        {
            get { return "SGSearch"; }
        }

        #endregion

        #region ISharedRegionModule Members

        public void PostInitialise()
        {
            // NoOp
        }

        #endregion

        #region EventHandlers
        private void OnNewClient(IClientAPI client)
        {
            if (m_debugEnabled) m_log.DebugFormat("[SGSearch]: {0} called", System.Reflection.MethodBase.GetCurrentMethod().Name);

            client.OnDirFindQuery += OnDirFindQuery;
        }


        void OnDirFindQuery(IClientAPI remoteClient, UUID queryID, string queryText, uint queryFlags, int queryStart)
        {
            if (((DirFindFlags)queryFlags & DirFindFlags.Groups) == DirFindFlags.Groups)
            {
            }
            else if (((DirFindFlags)queryFlags & DirFindFlags.People) == DirFindFlags.People)
            {
                List<UserAccount> accounts = m_accountService.GetUserAccounts(UUID.Zero, queryText);

                DirPeopleReplyData[] reply = new DirPeopleReplyData[accounts.Count];
                int acccountNum = 0;
                foreach (UserAccount account in accounts)
                {
                    reply[acccountNum] = new DirPeopleReplyData();
                    reply[acccountNum].agentID = account.PrincipalID;
                    reply[acccountNum].firstName = account.FirstName;
                    reply[acccountNum].lastName = account.LastName;
                    reply[acccountNum].group = account.UserTitle;
                    reply[acccountNum].online = false;
                    reply[acccountNum].reputation = 0;
                }

                remoteClient.SendDirPeopleReply(queryID, reply);
            }
        }

        #endregion


        #region Simian Util Methods
        private bool SimianAddGeneric(UUID ownerID, string type, string key, OSDMap map)
        {
            if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  {0} called ({1},{2},{3})", System.Reflection.MethodBase.GetCurrentMethod().Name, ownerID, type, key);

            string value = OSDParser.SerializeJsonString(map);

            if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  value: {0}", value);

            NameValueCollection RequestArgs = new NameValueCollection
            {
                { "RequestMethod", "AddGeneric" },
                { "OwnerID", ownerID.ToString() },
                { "Type", type },
                { "Key", key },
                { "Value", value}
            };


            OSDMap Response = CachedPostRequest(RequestArgs);
            if (Response["Success"].AsBoolean())
            {
                return true;
            }
            else
            {
                m_log.WarnFormat("[SIMIAN GROUPS CONNECTOR]: Error {0}, {1}, {2}, {3}", ownerID, type, key, Response["Message"]);
                return false;
            }
        }

        /// <summary>
        /// Returns the first of possibly many entries for Owner/Type pair
        /// </summary>
        private bool SimianGetFirstGenericEntry(UUID ownerID, string type, out string key, out OSDMap map)
        {
            if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  {0} called ({1},{2})", System.Reflection.MethodBase.GetCurrentMethod().Name, ownerID, type);

            NameValueCollection RequestArgs = new NameValueCollection
            {
                { "RequestMethod", "GetGenerics" },
                { "OwnerID", ownerID.ToString() },
                { "Type", type }
            };


            OSDMap Response = CachedPostRequest(RequestArgs);
            if (Response["Success"].AsBoolean() && Response["Entries"] is OSDArray)
            {
                OSDArray entryArray = (OSDArray)Response["Entries"];
                if (entryArray.Count >= 1)
                {
                    OSDMap entryMap = entryArray[0] as OSDMap;
                    key = entryMap["Key"].AsString();
                    map = (OSDMap)OSDParser.DeserializeJson(entryMap["Value"].AsString());

                    if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  Generics Result {0}", entryMap["Value"].AsString());

                    return true;
                }
                else
                {
                    if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  No Generics Results");
                }
            }
            else
            {
                m_log.WarnFormat("[SIMIAN GROUPS CONNECTOR]: Error retrieving group info ({0})", Response["Message"]);
            }
            key = null;
            map = null;
            return false;
        }
        private bool SimianGetFirstGenericEntry(string type, string key, out UUID ownerID, out OSDMap map)
        {
            if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  {0} called ({1},{2})", System.Reflection.MethodBase.GetCurrentMethod().Name, type, key);


            NameValueCollection RequestArgs = new NameValueCollection
            {
                { "RequestMethod", "GetGenerics" },
                { "Type", type },
                { "Key", key}
            };


            OSDMap Response = CachedPostRequest(RequestArgs);
            if (Response["Success"].AsBoolean() && Response["Entries"] is OSDArray)
            {
                OSDArray entryArray = (OSDArray)Response["Entries"];
                if (entryArray.Count >= 1)
                {
                    OSDMap entryMap = entryArray[0] as OSDMap;
                    ownerID = entryMap["OwnerID"].AsUUID();
                    map = (OSDMap)OSDParser.DeserializeJson(entryMap["Value"].AsString());

                    if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  Generics Result {0}", entryMap["Value"].AsString());

                    return true;
                }
                else
                {
                    if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  No Generics Results");
                }
            }
            else
            {
                m_log.WarnFormat("[SIMIAN GROUPS CONNECTOR]: Error retrieving group info ({0})", Response["Message"]);
            }
            ownerID = UUID.Zero;
            map = null;
            return false;
        }

        private bool SimianGetGenericEntry(UUID ownerID, string type, string key, out OSDMap map)
        {
            if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  {0} called ({1},{2},{3})", System.Reflection.MethodBase.GetCurrentMethod().Name, ownerID, type, key);

            NameValueCollection RequestArgs = new NameValueCollection
            {
                { "RequestMethod", "GetGenerics" },
                { "OwnerID", ownerID.ToString() },
                { "Type", type },
                { "Key", key}
            };


            OSDMap Response = CachedPostRequest(RequestArgs);
            if (Response["Success"].AsBoolean() && Response["Entries"] is OSDArray)
            {
                OSDArray entryArray = (OSDArray)Response["Entries"];
                if (entryArray.Count == 1)
                {
                    OSDMap entryMap = entryArray[0] as OSDMap;
                    key = entryMap["Key"].AsString();
                    map = (OSDMap)OSDParser.DeserializeJson(entryMap["Value"].AsString());

                    if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  Generics Result {0}", entryMap["Value"].AsString());

                    return true;
                }
                else
                {
                    if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  No Generics Results");
                }
            }
            else
            {
                m_log.WarnFormat("[SIMIAN GROUPS CONNECTOR]: Error retrieving group info ({0})", Response["Message"]);
            }
            map = null;
            return false;
        }

        private bool SimianGetGenericEntries(UUID ownerID, string type, out Dictionary<string, OSDMap> maps)
        {
            if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  {0} called ({1},{2})", System.Reflection.MethodBase.GetCurrentMethod().Name, ownerID, type);

            NameValueCollection requestArgs = new NameValueCollection
            {
                { "RequestMethod", "GetGenerics" },
                { "OwnerID", ownerID.ToString() },
                { "Type", type }
            };



            OSDMap response = CachedPostRequest(requestArgs);
            if (response["Success"].AsBoolean() && response["Entries"] is OSDArray)
            {
                maps = new Dictionary<string, OSDMap>();

                OSDArray entryArray = (OSDArray)response["Entries"];
                foreach (OSDMap entryMap in entryArray)
                {
                    if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  Generics Result {0}", entryMap["Value"].AsString());
                    maps.Add(entryMap["Key"].AsString(), (OSDMap)OSDParser.DeserializeJson(entryMap["Value"].AsString()));
                }
                if (maps.Count == 0)
                {
                    if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  No Generics Results");
                }

                return true;
            }
            else
            {
                maps = null;
                m_log.WarnFormat("[SIMIAN GROUPS CONNECTOR]: Error retrieving group info ({0})", response["Message"]);
            }
            return false;
        }
        private bool SimianGetGenericEntries(string type, string key, out Dictionary<UUID, OSDMap> maps)
        {
            if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  {0} called ({1},{2})", System.Reflection.MethodBase.GetCurrentMethod().Name, type, key);

            NameValueCollection requestArgs = new NameValueCollection
            {
                { "RequestMethod", "GetGenerics" },
                { "Type", type },
                { "Key", key }
            };



            OSDMap response = CachedPostRequest(requestArgs);
            if (response["Success"].AsBoolean() && response["Entries"] is OSDArray)
            {
                maps = new Dictionary<UUID, OSDMap>();

                OSDArray entryArray = (OSDArray)response["Entries"];
                foreach (OSDMap entryMap in entryArray)
                {
                    if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  Generics Result {0}", entryMap["Value"].AsString());
                    maps.Add(entryMap["OwnerID"].AsUUID(), (OSDMap)OSDParser.DeserializeJson(entryMap["Value"].AsString()));
                }
                if (maps.Count == 0)
                {
                    if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  No Generics Results");
                }
                return true;
            }
            else
            {
                maps = null;
                m_log.WarnFormat("[SIMIAN-GROUPS-CONNECTOR]: Error retrieving group info ({0})", response["Message"]);
            }
            return false;
        }

        private bool SimianRemoveGenericEntry(UUID ownerID, string type, string key)
        {
            if (m_debugEnabled) m_log.InfoFormat("[SIMIAN-GROUPS-CONNECTOR]  {0} called ({1},{2},{3})", System.Reflection.MethodBase.GetCurrentMethod().Name, ownerID, type, key);

            NameValueCollection requestArgs = new NameValueCollection
            {
                { "RequestMethod", "RemoveGeneric" },
                { "OwnerID", ownerID.ToString() },
                { "Type", type },
                { "Key", key }
            };


            OSDMap response = CachedPostRequest(requestArgs);
            if (response["Success"].AsBoolean())
            {
                return true;
            }
            else
            {
                m_log.WarnFormat("[SIMIAN GROUPS CONNECTOR]: Error {0}, {1}, {2}, {3}", ownerID, type, key, response["Message"]);
                return false;
            }
        }
        #endregion

        #region CheesyCache
        OSDMap CachedPostRequest(NameValueCollection requestArgs)
        {
            // Immediately forward the request if the cache is disabled.
            if (m_cacheTimeout == 0)
            {
                return WebUtil.PostToService(m_simianGridServerURI, requestArgs);
            }

            // Check if this is an update or a request
            if (requestArgs["RequestMethod"] == "RemoveGeneric"
                || requestArgs["RequestMethod"] == "AddGeneric"
                )
            {
                // Any and all updates cause the cache to clear
                m_memoryCache.Clear();

                // Send update to server, return the response without caching it
                return WebUtil.PostToService(m_simianGridServerURI, requestArgs);

            }

            // If we're not doing an update, we must be requesting data

            // Create the cache key for the request and see if we have it cached
            string CacheKey = WebUtil.BuildQueryString(requestArgs);
            OSDMap response = null;
            if (!m_memoryCache.TryGetValue(CacheKey, out response))
            {
                // if it wasn't in the cache, pass the request to the Simian Grid Services 
                response = WebUtil.PostToService(m_simianGridServerURI, requestArgs);

                // and cache the response
                m_memoryCache.AddOrUpdate(CacheKey, response, TimeSpan.FromSeconds(m_cacheTimeout));
            }

            // return cached response
            return response;
        }
        #endregion


    }
}
