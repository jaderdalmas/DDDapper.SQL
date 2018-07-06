using Back.Repositories;
using System;

namespace Back.Services
{
    public class BaseGuidService<T> : BaseService<T>
    {
        protected String userHostAddress;
        protected Guid userHostName;

        //protected Person logedUser;

        public BaseGuidService(BaseRepository<T> baseRepository, String userHostAddress, Guid userHostName)://, Person logedUser = null) :
            base(baseRepository, ServType.guidType)
        {
            //this.logedUser = (logedUser != null ? logedUser : (new PersonRepository()).GetSystemPerson());

            this.userHostAddress = userHostAddress;
            this.userHostName = userHostName;// (!Guid.Empty.Equals(userHostName) ? userHostName : this.logedUser.id);
        }

        public override void Creating<T>(ref T value)
        {
            value.GetType().GetMethod("Create", new Type[2] { typeof(String), typeof(Guid) }).Invoke(value, new object[2] { userHostAddress, userHostName });

            base.Creating<T>(ref value);
        }

        public override void Updating<T>(ref T value)
        {
            value.GetType().GetMethod("Update", new Type[2] { typeof(String), typeof(Guid) }).Invoke(value, new object[2] { userHostAddress, userHostName });

            base.Updating<T>(ref value);
        }

        public override void Deleting<T>(ref T value)
        {
            value.GetType().GetMethod("Delete", new Type[2] { typeof(String), typeof(Guid) }).Invoke(value, new object[2] { userHostAddress, userHostName });

            base.Deleting<T>(ref value);
        }
    }
}
