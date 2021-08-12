using Soneta.Business;
using Soneta.Business.App;
using Soneta.Business.Db;
using Soneta.Business.Licence;
using Soneta.Core;
using Soneta.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace PrzykladHandel
{
    public static class Extensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static ObservableCollection<T> ToObservableCollection<T>(this IList<T> list)
        {
            return new ObservableCollection<T>(list);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> list)
        {
            return new ObservableCollection<T>(list);
        }

        /// <summary>
        /// Logowanie do bazy danych jako harmonogram zadań
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static Login LoginAsScheduler(this Database database)
        {
            LoginParameters parameters = new LoginParameters
            {
                Mode = AuthenticationType.UserPassword,
                Operator = "Harmonogram Zadań",
                Password = "enova.123456",
                ClientUniqueID = LicenceInfo.SchedulerClientId.ToString()
            };

            return database.Login(parameters);
        }

        /// <summary>
        /// Załadowanie algorytmów Sonety
        /// </summary>
        /// <param name="login"></param>
        internal static void CacheAlgorithms(this Login login)
        {
            using (Session session = login.CreateSession(false, false))
            {
                CoreModule coreModule = CoreModule.GetInstance(session);
                MethodInfo method = coreModule.TuplesDefs.GetType().GetMethod("LoadAssembly", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null)
                {
                    throw new Exception("Invalid version of Soneta.Core.dll");
                }
                _ = method.Invoke(coreModule.TuplesDefs, new object[] { });
                BusinessModule businessModule = BusinessModule.GetInstance(session);
                method = businessModule.FeatureDefs.GetType().GetMethod("CheckAlgorithms", BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null)
                {
                    throw new Exception("Invalid version of Soneta.Business.dll");
                }
                _ = method.Invoke(businessModule.FeatureDefs, new object[] { });

                session.Save();
            }
        }
    }
}
