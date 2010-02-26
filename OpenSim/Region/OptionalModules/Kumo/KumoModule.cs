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
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;


namespace OpenSim.Region.CoreModules.Avatar.Groups
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class GroupsModule : ISharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private List<Scene> m_SceneList = new List<Scene>();

        private TimeSpan m_Reservation = TimeSpan.Zero;
        private System.Timers.Timer m_ReservationTimer;
        private double m_ShutdownTimeout;
        private double m_SaveOARTimeout;

        private List<Guid> m_OARQueue;

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
            
            
            IConfig kumoConfig = config.Configs["Kumo"];

            if (kumoConfig == null)
            {
                m_log.Info("[KUMO]: No configuration found. Using defaults");
            }
            else
            {
                // TODO: Load Module specific config
                m_SaveOARTimeout  = TimeSpan.FromMinutes(60).TotalMilliseconds;
                m_ShutdownTimeout = TimeSpan.FromMinutes(3).TotalMilliseconds;

                m_Reservation = TimeSpan.FromMinutes(5.0);

                m_ReservationTimer = new System.Timers.Timer();
                m_ReservationTimer.Elapsed += new System.Timers.ElapsedEventHandler(m_ReservationTimer_Elapsed);
                m_ReservationTimer.AutoReset = false;
                m_ReservationTimer.Interval = m_Reservation.TotalMilliseconds;
                m_ReservationTimer.Start();

                m_OARQueue = new List<Guid>();
            }
        }

        void m_ReservationTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs eva)
        {
            foreach (Scene scene in m_SceneList)
            {
                IRegionArchiverModule archiver = scene.RequestModuleInterface<IRegionArchiverModule>();
                if (archiver != null)
                {
                    Guid requestID = Guid.NewGuid();

                    string fileName = scene.RegionInfo.RegionName.Replace(":", "_");
                    foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                        fileName = fileName.Replace(c, '_');

                    fileName += ".oar";

                    m_OARQueue.Add(requestID);

                    archiver.ArchiveRegion(fileName, requestID);
                }
            }


            System.Timers.Timer forceDownTimer = new System.Timers.Timer(m_ShutdownTimeout);
            forceDownTimer.Elapsed += delegate(object s, System.Timers.ElapsedEventArgs e) {Environment.Exit(0);};
            forceDownTimer.Start();
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void AddRegion(Scene scene)
        {
            lock (m_SceneList)
            {
                if (!m_SceneList.Contains(scene))
                {
                    m_SceneList.Add(scene);

                    scene.EventManager.OnOarFileSaved += new EventManager.OarFileSaved(EventManager_OnOarFileSaved);
                }
            }
        }

        void EventManager_OnOarFileSaved(Guid guid, string message)
        {
            if (m_OARQueue.Contains(guid))
            {
                m_OARQueue.Remove(guid);

                // Only check to see if the queue is empty, after successfully
                // removing an item from the queue.  Otherwise the save oar might
                // have been triggered from the console.
                if (m_OARQueue.Count == 0)
                {
                    Util.FireAndForget(delegate { MainConsole.Instance.RunCommand("quit"); });

                    System.Timers.Timer forceDownTimer = new System.Timers.Timer(m_ShutdownTimeout);
                    forceDownTimer.Elapsed += delegate(object s, System.Timers.ElapsedEventArgs e) { Environment.Exit(0); };
                    forceDownTimer.Start();
                }
            }
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
            lock (m_SceneList)
            {
                if (m_SceneList.Contains(scene))
                    m_SceneList.Remove(scene);
            }
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "KumoModule"; }
        }

        #endregion


    }
}
