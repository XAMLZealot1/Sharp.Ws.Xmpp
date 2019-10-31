using System;
using System.Collections.Generic;
using System.Text;

using log4net;

namespace Sharp.Xmpp
{
    /// <summary>
    /// Class used to manage logger objects / information
    /// </summary>
    public static class LogConfigurator
    {
        static private Boolean repositoryCreated = false;
        static private String repositoryName = "RAINBOW_REPOSITORY_LOGGER";

        /// <summary>
        /// To set the name of the repositoru used for logging purpose
        /// </summary>
        /// <param name="name">Repository's name</param>
        static public void SetRepositoryName(String name)
        {
            repositoryName = name;
        }

        /// <summary>
        /// To get the name of the repositoru used for logging purpose
        /// </summary>
        /// <returns><see cref="String"/>Repository's name</returns>
        static public String GetRepositoryName()
        {
            return repositoryName;
        }

        /// <summary>
        /// To centralize the way to get ILog object in all the SDK
        /// Useful too if you want to use the same repository to log information from your own application
        /// </summary>
        static public log4net.Repository.ILoggerRepository GetRepository()
        {
            log4net.Repository.ILoggerRepository repository = null;

            // Try to get our repository
            try
            {
                repository = log4net.LogManager.GetRepository(repositoryName);
            }
            catch { }

            // If not found, we need to create it
            if (repository == null)
            {
                repository = log4net.LogManager.CreateRepository(repositoryName);
                repositoryCreated = true;
            }
            return repository;
        }

        /// <summary>
        /// To centralize the way to get ILog object in all the SDK
        /// Useful too if you want to use the same repository to log information from your own application
        /// </summary>
        static public ILog GetLogger(Type className)
        {
            if (!repositoryCreated)
                GetRepository();

            return LogManager.GetLogger(repositoryName, className);
        }
    }
}
