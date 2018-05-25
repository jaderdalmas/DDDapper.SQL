using System;

namespace Back.Models
{
    public class BaseModel
    {
        /// <summary>
        /// 0 to inactive | 1 to active | 2 to pending | 3 to merged
        /// </summary>
        public int active { get; set; }

        /// <summary>
        /// IP of the updating user
        /// </summary>
        public String hostAddress { get; set; }

        /// <summary>
        /// Last update date
        /// </summary>
        public DateTime date { get; set; }

        /// <summary>
        /// Id of the updating user
        /// </summary>
        public Guid userHost { get; set; }

        public BaseModel() { }

        public BaseModel(BaseModel control)
        {
            this.active = control.active;
            this.hostAddress = control.hostAddress;
            this.date = control.date;
            this.userHost = control.userHost;
        }

        /// <summary>
        /// Set active to true
        /// </summary>
        /// <param name="UserHostAddress">IP</param>
        /// <param name="UserHostName">Loged User Guid</param>
        public void Create(String UserHostAddress, Guid UserHostName)
        {
            active = 1;

            MetaData(UserHostAddress, UserHostName);
        }

        /// <summary>
        /// Set active to true or sent parameter
        /// </summary>
        /// <param name="UserHostAddress">IP</param>
        /// <param name="UserHostName">Loged User Guid</param>
        public void Update(String UserHostAddress, Guid UserHostName, int controlActive = 1)
        {
            active = controlActive;

            MetaData(UserHostAddress, UserHostName);
        }

        /// <summary>
        /// Override attributes
        /// </summary>
        /// <param name="entry">obj to get attributes from</param>
        public void Update(BaseModel entry)
        {
            active = entry.active;
            hostAddress = entry.hostAddress;
            date = entry.date;
            userHost = entry.userHost;
        }

        /// <summary>
        /// Set active to false
        /// </summary>
        /// <param name="UserHostAddress">IP</param>
        /// <param name="UserHostName">Loged User Guid</param>
        public void Delete(String UserHostAddress, Guid UserHostName)
        {
            active = 0;

            MetaData(UserHostAddress, UserHostName);
        }

        /// <summary>
        /// Verify if the entry is the last version of the row
        /// </summary>
        /// <param name="control">obj to be compared</param>
        /// <returns>true if last version</returns>
        public bool RowVersion(BaseModel control)
        {
            //this.date = this.date.AddTicks(-(this.date.Ticks % 1000000));
            //control.date = control.date.AddTicks(-(control.date.Ticks % 1000000));

            if (!this.date.Year.Equals(control.date.Year)) { return false; }
            if (!this.date.Month.Equals(control.date.Month)) { return false; }
            if (!this.date.Day.Equals(control.date.Day)) { return false; }
            if (!this.date.Hour.Equals(control.date.Hour)) { return false; }
            if (!this.date.Minute.Equals(control.date.Minute)) { return false; }
            if (!this.date.Second.Equals(control.date.Second)) { return false; }

            return true;
        }

        /// <summary>
        /// Update the entry
        /// </summary>
        /// <param name="UserHostAddress">IP</param>
        /// <param name="UserHostName">Loged User Guid</param>
        private void MetaData(String UserHostAddress, Guid UserHostName)
        {
            hostAddress = UserHostAddress;
            date = DateTime.UtcNow;
            userHost = UserHostName;
        }

        /// <summary>
        /// Compare this to an obj
        /// </summary>
        /// <param name="obj">object to be compared</param>
        /// <returns>true to equals</returns>
        public override bool Equals(object obj)
        {
            if (obj is BaseModel)
            {
                BaseModel control = obj as BaseModel;

                if (control.active != active) { return false; }
                if (control.hostAddress != hostAddress) { return false; }
                if (control.date != date) { return false; }
                if (control.userHost != userHost) { return false; }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Get a HashCode combining the attributes
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            int hash = 0;

            hash += active.GetHashCode();
            hash += hostAddress.GetHashCode();
            hash += date.GetHashCode();
            hash += userHost.GetHashCode();

            return hash;
        }
    }
}

