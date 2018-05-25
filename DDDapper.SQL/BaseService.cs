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
        public virtual object GetProperty<T>(T value, String idName = "")
        {
            string propName = (String.IsNullOrWhiteSpace(idName) ? _BaseRepository.nameId : idName);
            if (value.GetType().GetProperty(propName) == null)
                throw new BadParameterException();

            return value.GetType().GetProperty(propName).GetValue(value);
        }

        /// <summary>
        /// Get Properties from Values | if idName is empty, use RepNameId
        /// </summary>
        protected virtual List<object> GetProperty<T>(IEnumerable<T> values, IEnumerable<String> idNames = null)
        {
            List<object> result = new List<object>();
            foreach (T value in values)
                foreach (var idName in idNames ?? new List<String>() { _BaseRepository.nameId })
                    if (value.GetType().GetProperty(idName) != null)
                        result.Add(GetProperty(value, idName));
                    else
                        throw new BadParameterException();

            return result;
        }

        protected virtual T GetDataByProperties<T>(IEnumerable<T> values, IEnumerable<String> idNames = null, Boolean combine = false, Boolean all = false)
        {
            return Validate(_BaseRepository.GetDataTop1<T>(GetProperty(values, idNames).Select(x => x.ToString()), idNames, combine, all));
        }

        protected virtual IEnumerable<T> GetDatasByProperties<T>(IEnumerable<T> values, IEnumerable<String> idNames = null, Boolean combine = false, Boolean all = false)
        {
            return Validate(_BaseRepository.GetData<T>(GetProperty(values, idNames).Select(x => x.ToString()), idNames, combine, all));
        }

        protected virtual Boolean ExistsDatasByProperties<T>(IEnumerable<T> values, IEnumerable<String> idNames = null, Boolean combine = false, Boolean all = false)
        {
            return _BaseRepository.ExistsData<T>(GetProperty(values, idNames).Select(x => x.ToString()), idNames, values.Count(), combine, all);
        }

        #endregion

        public virtual int GetIdentity<T>()
        {
            return Validate(_BaseRepository.GetIdentity<T>());
        }

        public virtual IEnumerable<T> GetByIds<T>(IEnumerable<Guid> ids, Boolean all = false, String idName = "id")
        {
            ParameterValidate(ids);

            return Validate(_BaseRepository.GetData<T>(ids, all, idName));
        }

        public virtual IEnumerable<T> GetByIds<T>(IEnumerable<string> fields, Boolean all = false, String fieldName = "id", Boolean lower = true)
        {
            ParameterValidate(fields);

            return Validate(_BaseRepository.GetData<T>(fields, all, fieldName, lower));
        }

        public virtual IEnumerable<T> Get<T>(Boolean all = false)
        {
            return Validate(_BaseRepository.GetData<T>(all));
        }

        public virtual T Get<T>(Guid id, Boolean all = false)
        {
            ParameterValidate(id);

            return Validate(_BaseRepository.GetData<T>(id, all), true);
        }

        public virtual T Get<T>(int id, Boolean all = false)
        {
            ParameterValidate(id);

            return Validate(_BaseRepository.GetData<T>(id, all), true);
        }

        public virtual IEnumerable<T> Get<T>(String field, Boolean all = false, String fieldName = "id", Boolean lower = true)
        {
            ParameterValidate(field);

            return Validate(_BaseRepository.GetData<T>(field, all, fieldName, lower));
        }

        public virtual IEnumerable<T> GetNull<T>(Boolean all = false, String fieldName = "id", Boolean lower = true)
        {
            return Validate(_BaseRepository.GetDataNull<T>(all, fieldName, lower));
        }

        public virtual IEnumerable<T> GetNot<T>(String field, Boolean all = false, String fieldName = "id", Boolean lower = true)
        {
            ParameterValidate(field);

            return Validate(_BaseRepository.GetDataNot<T>(field, all, fieldName, lower));
        }

        public virtual IEnumerable<T> Get<T>(DateTime field, Boolean all = false, String fieldName = "id")
        {
            ParameterValidate(field);

            return Validate(_BaseRepository.GetData<T>(field, all, fieldName));
        }

        public virtual IEnumerable<T> Get<T>(Guid Id, IEnumerable<String> idNames, Boolean all = false)
        {
            if (!ParameterValidate(Id) || !ParameterValidate(idNames))
                throw new BadParameterException();

            return Validate(_BaseRepository.GetData<T>(new List<String>() { Id.ToString() }, idNames, false, all));
        }

        public virtual IEnumerable<T> Get<T>(IEnumerable<Guid> ids, IEnumerable<String> idNames, Boolean combine = false, Boolean all = false)
        {
            if (!ParameterValidate(ids) || !ParameterValidate(idNames) || ids.Count() != idNames.Count())
                throw new BadParameterException();

            return Validate(_BaseRepository.GetData<T>(ids.Select(x => x.ToString()), idNames, combine, all));
        }

        public virtual IEnumerable<T> Get<T>(IEnumerable<String> fields, IEnumerable<String> fieldNames, Boolean combine = false, Boolean all = false)
        {
            if (!ParameterValidate(fields) || !ParameterValidate(fieldNames) /*|| fields.Count() != fieldNames.Count()*/)
                throw new BadParameterException();

            return Validate(_BaseRepository.GetData<T>(fields, fieldNames, combine, all));
        }

        public virtual IEnumerable<T> GetLike<T>(String field, Boolean all = false, String fieldName = "id", Boolean lower = true)
        {
            ParameterValidate(field);

            return Validate(_BaseRepository.GetDataLike<T>(field, all, fieldName, lower));
        }

        #region Top 1

        public virtual T GetTop1<T>(String field, Boolean all = false, String fieldName = "id", Boolean lower = true)
        {
            ParameterValidate(field);

            return Validate(_BaseRepository.GetDataTop1<T>(field, all, fieldName, lower));
        }

        #endregion

        #region Post

        public virtual T Post<T>(T value, IEnumerable<String> idNames = null, Boolean combine = false)
        {
            ParameterValidate(value);

            if (ExistsDatasByProperties(new List<T>() { value }, idNames, combine))
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

        public virtual IEnumerable<T> PostMultiple<T>(IEnumerable<T> values, IEnumerable<String> idNames = null, Boolean combine = false)
        {
            ParameterValidate(values);

            if (values.GetType().GetProperty(_BaseRepository.nameId) != null && ExistsDatasByProperties(values, idNames, combine))
                throw new ConflictException();

            values.ToList().ForEach(value => Creating(ref value));
            if (_BaseRepository.PostDataMultiple<T>(values, idNames))
            {
                Created(ref values);
                return values;
            }
            else
                throw new NoRowAffectedException();
        }

        #endregion

        #region Put

        public virtual T Put<T>(T value, IEnumerable<String> idNames = null, Boolean combine = false, Boolean all = false)
        {
            idNames = _BaseRepository.ValidateIdNames(idNames, true);
            if (!ParameterValidate(value) || !ParameterValidate(idNames))
                throw new BadParameterException();

            T result = Validate(GetDataByProperties(new List<T>() { value }, idNames, combine, all), true);
            if (result == null) //!ExistsDatasByProperties(new List<T>() { value }, idNames, combine, all)
                throw new ConflictException();
            if (result.Equals(value))
                throw new NoContentException();
            if (value.GetType().BaseType.Equals(typeof(BaseModel)) && !(Boolean)result.GetType().GetMethod("RowVersion", new Type[1] { typeof(BaseModel) }).Invoke(result, new object[1] { value }))
            {
                throw new RowVersionException();
            }

            Updating(ref value);

            if (_BaseRepository.PutData<T>(value, idNames, combine))
            {
                Updated(ref value);
                return value;
            }
            else
                throw new NoRowAffectedException();
        }

        public virtual IEnumerable<T> PutMultiple<T>(IEnumerable<T> values, IEnumerable<String> idNames = null, Boolean combine = false, Boolean all = false)
        {
            idNames = _BaseRepository.ValidateIdNames(idNames, true);
            if (!ParameterValidate(values))
                throw new BadParameterException();

            IEnumerable<T> results = Validate(GetDatasByProperties(values, idNames, combine, all));
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

        public virtual T Delete<T>(Guid id)
        {
            ParameterValidate(id);
            T value = Validate(_BaseRepository.GetData<T>(id));

            Deleting(ref value);

            if (_BaseRepository.PutData<T>(value))
            {
                Deleted(ref value);
                return value;
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

        public virtual T Delete<T>(IEnumerable<Guid> ids, IEnumerable<String> idNames, Boolean combine = false, Boolean all = false)
        {
            if (!ParameterValidate(ids) || !ParameterValidate(idNames) || ids.Count() != idNames.Count())
                throw new BadParameterException();

            T value = Validate(_BaseRepository.GetData<T>(ids.Select(x => x.ToString()), idNames, combine, all).FirstOrDefault());

            Deleting(ref value);

            if (_BaseRepository.PutData<T>(value, idNames, combine))
            {
                Deleted(ref value);
                return value;
            }
            else
                throw new NoRowAffectedException();
        }

        public virtual T Drop<T>(Guid id)
        {
            ParameterValidate(id);
            T value = Validate(_BaseRepository.GetData<T>(id));

            Deleting(ref value);

            if (_BaseRepository.DeleteData<T>(value))
            {
                Deleted(ref value);
                throw new NoContentException();
            }
            else
                throw new NoRowAffectedException();
        }

        public virtual Boolean Drop<T>(IEnumerable<Guid> ids)
        {
            ParameterValidate(ids);

            if (_BaseRepository.DeleteData<T>(ids))
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

        public virtual T Drop<T>(IEnumerable<Guid> ids, IEnumerable<String> idNames, Boolean combine = false, Boolean all = false)
        {
            if (!ParameterValidate(ids) || !ParameterValidate(idNames) || ids.Count() != idNames.Count())
                throw new BadParameterException();

            T value = Validate(_BaseRepository.GetData<T>(ids.Select(x => x.ToString()), idNames, combine, all).FirstOrDefault());

            Deleting(ref value);

            if (_BaseRepository.DeleteData<T>(value, idNames, combine))
            {
                Deleted(ref value);
                throw new NoContentException();
            }
            else
                throw new NoRowAffectedException();
        }

        public virtual T GetHist<T>(Guid id, Boolean all = false)
        {
            ParameterValidate(id);

            return Validate(_BaseRepository.GetHist<T>(id, all), true);
        }

        public virtual IEnumerable<T> GetByHist<T>(Guid id, Boolean all = false, String idName = "id")
        {
            return GetByHist<T>(id.ToString(), all, idName, false, true);
        }

        public virtual IEnumerable<T> GetByHist<T>(int id, Boolean all = false, String idName = "id")
        {
            return GetByHist<T>(id.ToString(), all, idName, false, true);
        }

        public virtual IEnumerable<T> GetByHist<T>(String field, Boolean all = false, String fieldName = "id", Boolean lower = true, Boolean isImportant = false)
        {
            ParameterValidate(field);

            return Validate(_BaseRepository.GetByHist<T>(field, all, fieldName, lower), isImportant);
        }

        public virtual IEnumerable<T> GetByHist<T>(IEnumerable<Guid> ids, IEnumerable<String> idNames, Boolean combine = false)
        {
            if (!ParameterValidate(ids) || !ParameterValidate(idNames) || ids.Count() != idNames.Count())
                throw new BadParameterException();

            return Validate(_BaseRepository.GetByHist<T>(ids, idNames, combine));
        }

        #region UnitTest

        public void CleanIntegrationTestData(Boolean all = true)
        {
            _BaseRepository.CleanIntegrationTestData<T>(all);
        }

        #endregion
    }
}