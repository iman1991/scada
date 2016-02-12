﻿/*
 * Copyright 2016 Mikhail Shiryaev
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : ScadaWebCommon
 * Summary  : Common data of the web application
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2005
 * Modified : 2016
 */

using Scada.Client;
using Scada.Data;
using System;
using System.Text;
using System.Web;
using Utils;

namespace Scada.Web
{
    /// <summary>
    /// Common data of the web application
    /// <para>Общие данные веб-приложения</para>
    /// </summary>
    public static class AppData
	{
        /// <summary>
        /// Имя файла журнала приложения без директории
        /// </summary>
        public const string LogFileName = "ScadaWeb.log";

        private static readonly object appDataLock; // объект для синхронизации доступа к данным приложения

        private static DateTime scadaDataDictAge;   // время изменения файла словаря ScadaData
        private static DateTime scadaWebDictAge;    // время изменения файла словаря ScadaWeb


        /// <summary>
        /// Статический конструктор
        /// </summary>
        static AppData()
		{
            appDataLock = new object();

            scadaDataDictAge = DateTime.MinValue;
            scadaWebDictAge = DateTime.MinValue;

            Inited = false;
            AppDirs = new AppDirs();
            Log = new Log(Log.Formats.Full);
            WebSettings = new WebSettings();
            ServerComm = new ServerComm(WebSettings.CommSettings, Log);
            DataCache dataCache = new DataCache(ServerComm, Log);
            DataAccess = new DataAccess(dataCache, Log);
            ViewCache = new ViewCache(ServerComm, Log);
        }


        /// <summary>
        /// Получить признак инициализации общих данных приложения
        /// </summary>
        public static bool Inited { get; private set; }

        /// <summary>
        /// Получить директории приложения
        /// </summary>
        public static AppDirs AppDirs { get; private set; }
        
        /// <summary>
        /// Получить журнал приложения
        /// </summary>
        public static Log Log { get; private set; }

        /// <summary>
        /// Получить настройки веб-приложения
        /// </summary>
        public static WebSettings WebSettings { get; private set; }

        /// <summary>
        /// Получить объект для обмена данными с сервером
        /// </summary>
        public static ServerComm ServerComm { get; private set; }

        /// <summary>
        /// Получить объект для потокобезопасного доступа к данным кеша клиентов
        /// </summary>
        public static DataAccess DataAccess { get; private set; }

        /// <summary>
        /// Получить кэш представлений
        /// </summary>
        public static ViewCache ViewCache { get; private set; }


        /// <summary>
        /// Обновить словари веб-приложения
        /// </summary>
        private static void RefreshDictionaries()
        {
            // обновление словаря ScadaData
            bool reloaded;
            string msg;
            bool refreshOK = Localization.RefreshDictionary(AppDirs.LangDir, "ScadaData", 
                ref scadaDataDictAge, out reloaded, out msg);
            Log.WriteAction(msg, refreshOK ? Log.ActTypes.Action : Log.ActTypes.Error);

            if (reloaded)
                CommonPhrases.Init();

            // обновление словаря ScadaWeb
            refreshOK = Localization.RefreshDictionary(AppDirs.LangDir, "ScadaWeb", 
                ref scadaWebDictAge, out reloaded, out msg);
            Log.WriteAction(msg, refreshOK ? Log.ActTypes.Action : Log.ActTypes.Error);

            if (reloaded)
                WebPhrases.Init();
        }
        
        /// <summary>
        /// Обновить настройки веб-приложения
        /// </summary>
        private static void RefreshWebSettings(out bool reloaded, out bool commSettingsChanged)
        {
            reloaded = false;
            commSettingsChanged = false;
        }


        /// <summary>
        /// Инициализировать общие данные веб-приложения
        /// </summary>
        public static void Init()
        {
            ScadaWebUtils.CheckHttpContext();
            Init(HttpContext.Current.Request.PhysicalApplicationPath);
        }
        
        /// <summary>
        /// Инициализировать общие данные веб-приложения
        /// </summary>
        public static void Init(string webAppDir)
        {
            lock (appDataLock)
            {
                if (!Inited)
                {
                    Inited = true;

                    // инициализация директорий приложения
                    AppDirs.Init(webAppDir);

                    // настройка журнала приложения
                    Log.FileName = AppDirs.LogDir + LogFileName;
                    Log.Encoding = Encoding.UTF8;
                    Log.WriteBreak();
                    Log.WriteAction(Localization.UseRussian ?
                        "Инициализация общих данных веб-приложения" :
                        "Initialize common web application data");
                }

                // обновление данных веб-приложения
                Refresh();
            }
        }

        /// <summary>
        /// Обновить данные веб-приложения
        /// </summary>
        public static void Refresh()
        {
            lock (appDataLock)
            {
                if (Inited)
                {
                    // обновление словарей
                    RefreshDictionaries();
                }
            }
        }

        /// <summary>
        /// Проверить правильность имени и пароля пользователя
        /// </summary>
        public static bool CheckUser(string username, string password, bool checkPassword, 
            out int roleID, out string errMsg)
        {
            if (checkPassword && string.IsNullOrEmpty(password))
            {
                roleID = BaseValues.Roles.Err;
                errMsg = WebPhrases.WrongPassword;
                return false;
            }
            else
            {
                if (ServerComm.CheckUser(username, checkPassword ? password : null, out roleID))
                {
                    if (roleID == BaseValues.Roles.Disabled)
                    {
                        errMsg = WebPhrases.NoRightsL;
                        return false;
                    }
                    else if (roleID == BaseValues.Roles.App)
                    {
                        errMsg = WebPhrases.IllegalRole;
                        return false;
                    }
                    else if (roleID == BaseValues.Roles.Err)
                    {
                        errMsg = WebPhrases.WrongPassword;
                        return false;
                    }
                    else
                    {
                        errMsg = "";
                        return true;
                    }
                }
                else
                {
                    errMsg = WebPhrases.ServerUnavailable;
                    return false;
                }
            }
        }
    }
}