using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jinxapp
{
    public class ApplicationService
    {
        private readonly static object lockObject = new object();
        private volatile static ApplicationService _services;
        private Dictionary<Type, object> serivceDics = new Dictionary<Type, object>();
        public static ApplicationService Services
        {
            get
            {
                lock (lockObject)
                {
                    if (_services == null)
                    {
                        _services = new ApplicationService();
                    }

                    return _services;
                }


            }
        }

        public TService Take<TService>() where TService : class
        {
            object service = null;
            serivceDics.TryGetValue(typeof(TService),out service);
            return (TService)service;
        }

        public void Add<TService>(TService service)
        {
            Type serviceType = typeof(TService); 
            if(!serivceDics.Keys.Contains(serviceType)){
                serivceDics.Add(serviceType,service);
            }

        }



    }
}
