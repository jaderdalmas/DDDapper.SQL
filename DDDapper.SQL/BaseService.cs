using Back.Exceptions;
using Back.Models;
using Back.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Back.Services
{
    public enum ServType { guidType, hasntBaseModel, intType }

    public abstract class BaseService<T> : IDisposable
    {
        protected BaseRepository<T> _BaseRepository;

        protected ServType servType;

        public BaseService(BaseRepository<T> baseRepository, ServType servType = ServType.hasntBaseModel)
        {
            _BaseRepository = baseRepository;

            this.servType = servType;
        }

        public void Dispose()
        {
            _BaseRepository.Dispose();
        }

        #region Models Helpers

        public virtual void Creating<T>(ref T value) { }

        public virtual void Created<T>(ref T value) { }

        public virtual void Updating<T>(ref T value) { }

        public virtual void Updated<T>(ref T value) { }

        public virtual void Deleting<T>(ref T value) { }

        public virtual void Deleted<T>(ref T value) { }

        #endregion

        #region Helpers

        /// <summary>
        /// Validate Parameter
        /// </summary>
        public virtual Boolean ParameterValidate<T>(T value)
        {
            if (value == null)
                throw new BadParameterException();

            if (value is Guid && Guid.Empty.Equals(value))
                throw new BadParameterException();

            if (value is String && String.IsNullOrWhiteSpace(value.ToString()))
                throw new BadParameterException();

            return true;
        }

        /// <summary>
        /// Validate Parameter
        /// </summary>
        public virtual Boolean ParameterValidate<T>(IEnumerable<T> value)
        {
            if (value == null || value.Count().Equals(0))
                throw new BadParameterException();

            return true;
        }

        /// <summary>
        /// Validate Null (Exception due to importance)
        /// </summary>
        public virtual T Validate<T>(T value, Boolean isImportant = false)
        {
            if (value != null)
                return value;

            if (isImportant)
                throw new NotFoundException();
            else
                throw new NoContentException();
        }

        /// <summary>
        /// Validate Null or Empty
        /// </summary>
        public virtual IEnumerable<T> Validate<T>(IEnumerable<T> value)
        {
            if (value != null && value.Count() != 0)
                return value;

            throw new NoContentException();
        }

        /// <summary>
        /// Get Property from Value | if idName is empty, use RepNameId
        /// </summary>
        public virtual object GetProperty<T>(T value, String field = "")
        {
            string propName = (String.IsNullOrWhiteSpace(field) ? _BaseRepository.nameId : field);
            if (value.GetType().GetProperty(propName) == null)
                throw new BadParameterException();

            return value.GetType().GetProperty(propName).GetValue(value);
        }

        /// <summary>
        /// Get Properties from Values | if idName is empty, use RepNameId
        /// </summary>
        protected virtual List<object> GetProperty<T>(IEnumerable<T> values, IEnumerable<String> fields = null)
        {
            List<object> result = new List<object>();
            foreach (T value in values)
                foreach (var field in fields ?? new List<String>() { _BaseRepository.nameId })
                    if (value.GetType().GetProperty(field) != null)
                        result.Add(GetProperty(value, field));
                    else
                        throw new BadParameterException();

            return result;
        }

        protected virtual T GetDataByProperties<T>(IEnumerable<T> values, IEnumerable<String> fields = null, Boolean combine = false, Boolean all = false)
        {
            return Validate(_BaseRepository.GetDataTop1<T>(GetProperty(values, fields).Select(x => x.ToString()), fields, combine, all));
        }

        protected virtual IEnumerable<T> GetDatasByProperties<T>(IEnumerable<T> values, IEnumerable<String> fields = null, Boolean combine = false, Boolean all = false)
        {
            return Validate(_BaseRepository.GetData<T>(GetProperty(values, fields).Select(x => x.ToString()), fields, combine, all));
        }

        protected virtual Boolean ExistsDatasByProperties<T>(IEnumerable<T> values, IEnumerable<String> fields = null, Boolean combine = false, Boolean all = false)
        {
            return _BaseRepository.ExistsData<T>(GetProperty(values, fields).Select(x => x.ToString()), fields, values.Count(), combine, all);
        }

        #endregion

        public virtual int GetIdentity<T>()
        {
            return Validate(_BaseRepository.GetIdentity<T>());
        }

        public virtual IEnumerable<T> GetByIds<T>(IEnumerable<Guid> values, Boolean all = false, String field = "id")
        {
            ParameterValidate(values);

            return Validate(_BaseRepository.GetData<T>(values, all, field));
        }

        public virtual IEnumerable<T> GetByIds<T>(IEnumerable<string> values, Boolean all = false, String field = "id", Boolean lower = true)
        {
            ParameterValidate(values);

            return Validate(_BaseRepository.GetData<T>(values, all, field, lower));
        }

        public virtual IEnumerable<T> Get<T>(Boolean all = false)
        {
            return Validate(_BaseRepository.GetData<T>(all));
        }

        public virtual T Get<T>(Guid value, Boolean all = false)
        {
            ParameterValidate(value);

            return Validate(_BaseRepository.GetData<T>(value, all), true);
        }

        public virtual T Get<T>(int value, Boolean all = false)
        {
            ParameterValidate(value);

            return Validate(_BaseRepository.GetData<T>(value, all), true);
        }

        public virtual IEnumerable<T> Get<T>(String value, Boolean all = false, String field = "id", Boolean lower = true)
        {
            ParameterValidate(value);

            return Validate(_BaseRepository.GetData<T>(value, all, field, lower));
        }

        public virtual IEnumerable<T> GetNull<T>(Boolean all = false, String field = "id", Boolean lower = true)
        {
            return Validate(_BaseRepository.GetDataNull<T>(all, field, lower));
        }

        public virtual IEnumerable<T> GetNot<T>(String value, Boolean all = false, String field = "id", Boolean lower = true)
        {
            ParameterValidate(value);

            return Validate(_BaseRepository.GetDataNot<T>(value, all, field, lower));
        }

        public virtual IEnumerable<T> Get<T>(DateTime value, Boolean all = false, String field = "id")
        {
            ParameterValidate(value);

            return Validate(_BaseRepository.GetData<T>(value, all, field));
        }

        public virtual IEnumerable<T> Get<T>(Guid value, IEnumerable<String> fields, Boolean all = false)
        {
            if (!ParameterValidate(value) || !ParameterValidate(fields))
                throw new BadParameterException();

            return Validate(_BaseRepository.GetData<T>(new List<String>() { value.ToString() }, fields, false, all));
        }

        public virtual IEnumerable<T> Get<T>(IEnumerable<Guid> values, IEnumerable<String> fields, Boolean combine = false, Boolean all = false)
        {
            if (!ParameterValidate(values) || !ParameterValidate(fields) || values.Count() != fields.Count())
                throw new BadParameterException();

            return Validate(_BaseRepository.GetData<T>(values.Select(x => x.ToString()), fields, combine, all));
        }

        public virtual IEnumerable<T> Get<T>(IEnumerable<String> values, IEnumerable<String> fields, Boolean combine = false, Boolean all = false)
        {
            if (!ParameterValidate(values) || !ParameterValidate(fields)) /*|| fields.Count() != fieldNames.Count()*/
                throw new BadParameterException();

            return Validate(_BaseRepository.GetData<T>(values, fields, combine, all));
        }

        public virtual IEnumerable<T> GetLike<T>(String value, Boolean all = false, String field = "id", Boolean lower = true)
        {
            ParameterValidate(value);

            return Validate(_BaseRepository.GetDataLike<T>(value, all, field, lower));
        }

        #region Top 1

        public virtual T GetTop1<T>(String value, Boolean all = false, String field = "id", Boolean lower = true)
        {
            ParameterValidate(value);

            return Validate(_BaseRepository.GetDataTop1<T>(value, all, field, lower));
        }

        #endregion

        #region Post

        public virtual T Post<T>(T value, IEnumerable<String> fields = null, Boolean combine = false)
        {
            ParameterValidate(value);

            if (ExistsDatasByProperties(new List<T>() { value }, fields, combine))
                throw new ConflictException();

            Creating(ref value);
            if (_BaseRepository.PostData<T>(value))
            {
                Created(ref value);
                return value;
            }
            else
                throw new NoRowAffectedException();
        }

        public virtual IEnumerable<T> PostMultiple<T>(IEnumerable<T> values, IEnumerable<String> fields = null, Boolean combine = false)
        {
            ParameterValidate(values);

            if (values.GetType().GetProperty(_BaseRepository.nameId) != null && ExistsDatasByProperties(values, fields, combine))
                throw new ConflictException();

            values.ToList().ForEach(value => Creating(ref value));
            if (_BaseRepository.PostDataMultiple<T>(values, fields))
            {
                Created(ref values);
                return values;
            }
            else
                throw new NoRowAffectedException();
        }

        #endregion

        #region Put

        public virtual T Put<T>(T value, IEnumerable<String> fields = null, Boolean combine = false, Boolean all = false)
        {
            fields = _BaseRepository.ValidateIdNames(fields, true);
            if (!ParameterValidate(value) || !ParameterValidate(fields))
                throw new BadParameterException();

            T result = Validate(GetDataByProperties(new List<T>() { value }, fields, combine, all), true);
            if (result == null) //!ExistsDatasByProperties(new List<T>() { value }, idNames, combine, all)
                throw new ConflictException();
            if (result.Equals(value))
                throw new NoContentException();
            if (value.GetType().BaseType.Equals(typeof(BaseModel)) && !(Boolean)result.GetType().GetMethod("RowVersion", new Type[1] { typeof(BaseModel) }).Invoke(result, new object[1] { value }))
            {
                throw new RowVersionException();
            }

            Updating(ref value);

            if (_BaseRepository.PutData<T>(value, fields, combine))
            {
                Updated(ref value);
                return value;
            }
            else
                throw new NoRowAffectedException();
        }

        public virtual IEnumerable<T> PutMultiple<T>(IEnumerable<T> values, IEnumerable<String> fields = null, Boolean combine = false, Boolean all = false)
        {
            fields = _BaseRepository.ValidateIdNames(fields, true);
            if (!ParameterValidate(values))
                throw new BadParameterException();

            IEnumerable<T> results = Validate(GetDatasByProperties(values, fields, combine, all));
            for (int i = 0; i < values.Count(); i++)
            {
                T value = values.ElementAt(i);
                T result = results.Where(x => GetProperty(value).Equals(GetProperty(x))).FirstOrDefault();

                if (result == null)
                    throw new ConflictException();
                if (result == null || result.Equals(value))
                    throw new NoContentException();
                if (value.GetType().BaseType.Equals(typeof(BaseModel)) && !(Boolean)result.GetType().GetMethod("RowVersion", new Type[1] { typeof(BaseModel) }).Invoke(result, new object[1] { value }))
                    throw new RowVersionException();

                Updating(ref value);
            }

            if (_BaseRepository.PutDataMultiple<T>(values))
            {
                for (int i = 0; i < values.Count(); i++)
                {
                    T value = values.ElementAt(i);
                    Updated(ref value);
                }
                return values;
            }
            else
                throw new NoRowAffectedException();
        }

        #endregion

        public virtual T Delete<T>(Guid value)
        {
            ParameterValidate(value);
            T result = Validate(_BaseRepository.GetData<T>(value));

            Deleting(ref result);

            if (_BaseRepository.PutData<T>(result))
            {
                Deleted(ref result);
                return result;
            }
            else
                throw new NoRowAffectedException();
        }

        public virtual T Delete<T>(T value)
        {
            ParameterValidate(value);

            if (value.GetType().BaseType.Equals(typeof(BaseModel)))
            {
                T result = GetDataByProperties(new List<T>() { value });
                if (!(Boolean)result.GetType().GetMethod("RowVersion", new Type[1] { typeof(BaseModel) }).Invoke(result, new object[1] { value }))
                    throw new RowVersionException();
            }

            Deleting(ref value);

            if (_BaseRepository.PutData<T>(value))
            {
                Deleted(ref value);
                return value;
            }
            else
                throw new NoRowAffectedException();
        }

        public virtual T Delete<T>(IEnumerable<Guid> values, IEnumerable<String> fields, Boolean combine = false, Boolean all = false)
        {
            if (!ParameterValidate(values) || !ParameterValidate(fields) || values.Count() != fields.Count())
                throw new BadParameterException();

            T result = Validate(_BaseRepository.GetData<T>(values.Select(x => x.ToString()), fields, combine, all).FirstOrDefault());

            Deleting(ref result);

            if (_BaseRepository.PutData<T>(result, fields, combine))
            {
                Deleted(ref result);
                return result;
            }
            else
                throw new NoRowAffectedException();
        }

        public virtual T Drop<T>(Guid value)
        {
            ParameterValidate(value);
            T result = Validate(_BaseRepository.GetData<T>(value));

            Deleting(ref result);

            if (_BaseRepository.DeleteData<T>(result))
            {
                Deleted(ref result);
                throw new NoContentException();
            }
            else
                throw new NoRowAffectedException();
        }

        public virtual Boolean Drop<T>(IEnumerable<Guid> values)
        {
            ParameterValidate(values);

            if (_BaseRepository.DeleteData<T>(values))
                return true;
            else
                throw new NoRowAffectedException();
        }

        public virtual T Drop<T>(T value)
        {
            ParameterValidate(value);

            if (value.GetType().BaseType.Equals(typeof(BaseModel)))
            {
                T result = GetDataByProperties(new List<T>() { value });
                if (!(Boolean)result.GetType().GetMethod("RowVersion", new Type[1] { typeof(BaseModel) }).Invoke(result, new object[1] { value }))
                    throw new RowVersionException();
            }

            Deleting(ref value);

            if (_BaseRepository.DeleteData<T>(value))
            {
                Deleted(ref value);
                throw new NoContentException();
            }
            else
                throw new NoRowAffectedException();
        }

        public virtual IEnumerable<T> DropMultiple<T>(IEnumerable<T> values)
        {
            ParameterValidate(values);

            IEnumerable<T> results = GetDatasByProperties(values);
            for (int i = 0; i < values.Count(); i++)
            {
                T value = values.ElementAt(i);
                if (value.GetType().BaseType.Equals(typeof(BaseModel)))
                {
                    T result = results.Where(x => GetProperty(value).Equals(GetProperty(x))).FirstOrDefault();
                    if (!(Boolean)result.GetType().GetMethod("RowVersion", new Type[1] { typeof(BaseModel) }).Invoke(result, new object[1] { value }))
                        throw new RowVersionException();
                }

                Deleting(ref value);
            }

            if (_BaseRepository.DeleteDataMultiple<T>(values))
            {
                Deleted(ref values);
                throw new NoContentException();
            }
            else
                throw new NoRowAffectedException();
        }

        public virtual T Drop<T>(IEnumerable<Guid> values, IEnumerable<String> fields, Boolean combine = false, Boolean all = false)
        {
            if (!ParameterValidate(values) || !ParameterValidate(fields) || values.Count() != fields.Count())
                throw new BadParameterException();

            T result = Validate(_BaseRepository.GetData<T>(values.Select(x => x.ToString()), fields, combine, all).FirstOrDefault());

            Deleting(ref result);

            if (_BaseRepository.DeleteData<T>(result, fields, combine))
            {
                Deleted(ref result);
                throw new NoContentException();
            }
            else
                throw new NoRowAffectedException();
        }

        public virtual T GetHist<T>(Guid value, Boolean all = false)
        {
            ParameterValidate(value);

            return Validate(_BaseRepository.GetHist<T>(value, all), true);
        }

        public virtual IEnumerable<T> GetByHist<T>(Guid value, Boolean all = false, String fields = "id")
        {
            return GetByHist<T>(value.ToString(), all, fields, false, true);
        }

        public virtual IEnumerable<T> GetByHist<T>(int value, Boolean all = false, String fields = "id")
        {
            return GetByHist<T>(value.ToString(), all, fields, false, true);
        }

        public virtual IEnumerable<T> GetByHist<T>(String value, Boolean all = false, String field = "id", Boolean lower = true, Boolean isImportant = false)
        {
            ParameterValidate(value);

            return Validate(_BaseRepository.GetByHist<T>(value, all, field, lower), isImportant);
        }

        public virtual IEnumerable<T> GetByHist<T>(IEnumerable<Guid> value, IEnumerable<String> fields, Boolean combine = false)
        {
            if (!ParameterValidate(value) || !ParameterValidate(fields) || value.Count() != fields.Count())
                throw new BadParameterException();

            return Validate(_BaseRepository.GetByHist<T>(value, fields, combine));
        }

        #region UnitTest

        public void CleanIntegrationTestData(Boolean all = true)
        {
            _BaseRepository.CleanIntegrationTestData<T>(all);
        }

        #endregion
    }
}