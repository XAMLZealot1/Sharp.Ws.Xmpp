using System;
using System.Collections.Generic;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Sharp.Xmpp
{
    /// <summary>
    /// Class used to manage logger object / information
    /// 
    /// It's mandatory to configure first the logger object before to use the Sharp.Ws.Xmpp library.
    /// 
    /// The configuration is based on a valid XML String which provide all information necessary.
    /// 
    /// Cf: https://github.com/NLog/NLog/wiki
    /// 
    /// GetRepositoryName / SetRepositoryName allow to use a specific name for the log target and use the same one in you own project
    /// 
    /// </summary>
    public static class LogConfigurator
    {
        static private String repositoryName = "Sharp.Ws.Xmpp";

        /// <summary>
        /// To set the name of the repository used for logging purpose.
        /// </summary>
        /// <param name="name">Repository's name</param>
        static public void SetRepositoryName(String name)
        {
            repositoryName = name;
        }

        /// <summary>
        /// To get the name of the repository used for logging purpose
        /// 
        /// By default this name is "Sharp.Ws.Xmpp"
        /// </summary>
        /// <returns><see cref="String"/>Repository's name</returns>
        static public String GetRepositoryName()
        {
            return repositoryName;
        }

        /// <summary>
        /// Configure log configuration.
        /// 
        /// This XML String must be a valid configuration like the one you could define in NLog.config - Cf: https://github.com/NLog/NLog/wiki/Tutorial
        /// 
        /// This configuration must define at least on **target** with a name equals to the repository name specify using "SetRepositoryName" method
        /// </summary>
        /// <param name="xmlString">XML String use to configure logs</param>
        /// <returns><see cref="Boolean"/> - True if the configuration has been well taken into account</returns>
        static public Boolean Configure(String xmlString)
        {
            try
            {
                XmlLoggingConfiguration config = XmlLoggingConfiguration.CreateFromXmlString(xmlString);
                if (config.InitializeSucceeded == true)
                {
                    // Set configuration
                    NLog.LogManager.Configuration = config;

                    // Ensure to have a Target with a valid configuration AND with a name equals to "repositoryName"
                    IReadOnlyList<Target> targets = NLog.LogManager.Configuration.ConfiguredNamedTargets;
                    if ((targets != null) && (targets.Count > 0))
                    {
                        foreach (Target target in targets)
                        {
                            if (target.Name == repositoryName)
                                return true;
                        }
                    }
                }
            }
            catch
            {
                // Nothing to do more ...   
            }

            return false;
        }

        /// <summary>
        /// Get Logger object (based on the class name type specified)
        /// 
        /// Used this only after configured this obcjet using Configure method
        /// </summary>
        /// <param name="className">Type declaration</param>
        /// <returns><see cref="Logger"/> object</returns>
        static public Logger GetLogger(Type className, String loggerName = null)
        {
            Logger log;
            if(String.IsNullOrEmpty(loggerName))
                log = NLog.LogManager.GetLogger("*"); 
            else
                log = NLog.LogManager.GetLogger(loggerName);
            return log;
        }
    }
}
