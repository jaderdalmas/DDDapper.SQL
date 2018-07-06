using Back.Connection;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Back.Repositories
{
    public enum RepType { guidType, hasntHist, intType }

    public abstract class BaseRepository<T> : IDisposable
    {
        /// <summary>
        /// Table Prefix "dbo."
        /// </summary>
        protected string tablePrefix;
        /// <summary>
        /// Connection String "data base connection"
        /// </summary>
        private BaseConnection baseConnection;
        protected SqlConnection dbConnection { get { return baseConnection.DbConnection; } }

        /// <summary>
        /// Table Hist Sufix "_Hist"
        /// </summary>
        protected string histSufix;
        /// <summary>
        /// Hist Id "IdHist"
        /// </summary>
        public string histId;
        /// <summary>
        /// Table Id field Name "id"
        /// </summary>
        public string nameId;
        /// <summary>
        /// Table soft delete field "active = 1"
        /// </summary>
        public string activeId;
        /// <summary>
        /// Table order field Name "date desc"
        /// </summary>
        public string orderId;

        protected int Page { get { return 50; } }
        protected int PageSuggest { get { return 10; } }

        /// <summary>
        /// Table id type in hist config
        /// </summary>
        protected RepType repType;

        /// <summary>
        /// Fill Objects + Open Connection
        /// </summary>
        /// <param name="tablePrefix"></param>
        /// <param name="connectionStrings"></param>
        /// <param name="repType"></param>
        /// <param name="histSufix"></param>
        /// <param name="histId"></param>
        /// <param name="nameId"></param>
        /// <param name="activeId"></param>
        /// <param name="orderId"></param>
        public BaseRepository(String tablePrefix = "dbo.", String connectionStrings = "cnn", RepType repType = RepType.hasntHist, String histSufix = "Hist"
            , String histId = "IdHist", String nameId = "id", String activeId = "active = 1", String orderId = "date desc")
        {
            this.tablePrefix = tablePrefix;

            this.histSufix = histSufix;
            this.histId = histId;

            this.nameId = nameId;
            this.activeId = activeId;
            this.orderId = orderId;

            this.repType = repType;

            this.baseConnection = new BaseConnection(connectionStrings);
        }

        /// <summary>
        /// Dispose Objects
        /// </summary>
        public void Dispose()
        {
            baseConnection.Dispose();
        }

        #region Helpers

        /// <summary>
        /// Validate idNames and fill if necessary
        /// </summary>
        public List<String> ValidateIdNames(IEnumerable<String> fields, Boolean isNotNull = false)
        {
            List<String> values = new List<String>();
            if (fields != null) // if not null (add range)
                values.AddRange(fields);
            //if (ids.Any(x => x.Contains("id"))) // if has any id (add id)
            //    ids.Add("id");
            if (!values.Any() && (repType == RepType.intType || isNotNull)) // if id is int or isNotNull (add nameId)
                values.Add(nameId);

            return values.Distinct().ToList();
        }

        protected void GetFieldName(ref StringBuilder query, String field, Boolean lower = true)
        {
            if (lower)
                query.Append("lower(");

            query.Append(field);

            if (lower)
                query.Append(")");
        }

        protected void GetIdComparer(ref StringBuilder query, IEnumerable<String> fields = null, Boolean combine = false)
        {
            List<String> ids = ValidateIdNames(fields);

            if (combine)
                query.Append("(");
            for (int j = 0; j < (combine ? ids.Count() : 1); j++)
            {
                if (j != 0)
                    query.Append(" or ");

                query.Append("(");
                for (int i = 0; i < ids.Count(); i++)
                {
                    if (i != 0)
                        query.Append(" and ");

                    query.Append(ids[i]);
                    query.Append(" = @");
                    query.Append(ids[(i + j) % ids.Count()]);
                }
                query.Append(")");
            }
            if (combine)
                query.Append(")");
        }

        protected void GetOrderBy(ref StringBuilder query)
        {
            query.Append((String.IsNullOrWhiteSpace(orderId) ? "" : " order by " + orderId));
        }

        protected IEnumerable<String> GetProperties(Type t, String pk = "")
        {
            return GetProperties(t, new List<String>() { pk });
        }

        protected IEnumerable<String> GetProperties(Type t, IEnumerable<String> pk)
        {
            List<String> result = t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Select(x => x.Name).ToList();

            ValidateIdNames(pk).ForEach(x => result.Remove(x));

            return result;
        }

        protected void GetQueryHist<T>(ref StringBuilder query, T obj, bool isCreate = false)
        {
            if (repType != RepType.hasntHist)
            {
                IEnumerable<String> properties = GetProperties(typeof(T));

                query.AppendLine();
                query.Append("insert into ");
                query.Append(GetTableName(typeof(T), true));
                query.Append(" (");

                if (repType == RepType.guidType)
                    query.Append(this.histId + ",");

                query.Append(String.Join(",", properties.Select(x => String.Format("[{0}]", x))));
                query.Append(") values (");

                if (repType == RepType.guidType)
                    query.Append("'" + Guid.NewGuid().ToString() + "',");

                string props = String.Join(",", properties.Select(x => String.Format("@{0}", x)));
                if (repType == RepType.intType && isCreate)
                    props = props.Replace(nameId + ",", "@IDENTITY,");

                query.Append(props);
                query.Append(")");

            }
        }

        protected void GetQueryList(ref StringBuilder query, IEnumerable<int> values)
        {
            GetQueryList(ref query, values.Select(x => x.ToString()));
        }

        protected void GetQueryList(ref StringBuilder query, IEnumerable<Guid> values)
        {
            GetQueryList(ref query, values.Select(x => x.ToString()));
        }

        protected void GetQueryList(ref StringBuilder query, IEnumerable<String> values)
        {
            query.Append("('");
            query.Append(String.Join("','", values.Select(x => x.Replace("'", ""))));
            query.Append("')");
        }

        protected void GetQueryParameters(ref StringBuilder query, IEnumerable<String> values, IEnumerable<String> fields, Boolean combine = false)
        {
            bool isMultiple = (values.Count() / fields.Count()) != 1; // A1B1C1 - A2B2C2 - A3B3C3 | MultipleFields

            if (isMultiple) // Multiple Combines
                query.Append("(");
            for (int k = 0; k < (values.Count() / fields.Count()); k++)
            {
                if (k != 0)
                    query.Append(" or ");

                if (combine) // Field Combine
                    query.Append("(");
                for (int j = 0; j < (combine ? fields.Count() : 1); j++)
                {
                    if (j != 0)
                        query.Append(" or ");

                    query.Append("(");
                    for (int i = 0; i < fields.Count(); i++)
                    {
                        if (i != 0)
                            query.Append(" and ");

                        query.Append(fields.ElementAt(i));
                        query.Append(" = '");
                        query.Append(values.ElementAt(((i + j) % fields.Count()) + (k * fields.Count())).ToString());
                        query.Append("'");
                    }
                    query.Append(")");
                }
                if (combine)
                    query.Append(")");
            }
            if (isMultiple)
                query.Append(")");
        }

        protected String GetTableName(Type t, Boolean isHist = false)
        {
            string tableName = t.Name;
            tableName = (tableName.Contains("DTO") ? tableName.Substring(0, tableName.IndexOf("DTO")) : tableName);
            tableName = (tableName.Contains("Old") ? tableName.Substring(0, tableName.IndexOf("Old")).ToLower() : tableName);

            return tablePrefix + "[" + tableName + (isHist ? this.histSufix : "") + "]";
        }

        #endregion

        public virtual int GetIdentity<T>()
        {
            return dbConnection.ExecuteScalar<int>("select IDENT_CURRENT('" + GetTableName(typeof(T)) + "')");
        }

        /// <summary>
        /// Get All
        /// </summary>
        public virtual IEnumerable<T> GetData<T>(Boolean all = true)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T)));
            query.Append((all ? "" : " where " + activeId));
            GetOrderBy(ref query);

            return dbConnection.Query<T>(query.ToString());
        }

        /// <summary>
        /// Get By Ids
        /// </summary>
        public virtual IEnumerable<T> GetData<T>(IEnumerable<String> values, IEnumerable<String> fields, Boolean combine = false, Boolean all = false)
        {
            fields = ValidateIdNames(fields, true);
            if (values == null || !values.Any() || values.Count() % fields.Count() != 0)
                return null;

            StringBuilder query = new StringBuilder();
            query.Append("select ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append((all ? "" : activeId + " and "));
            GetQueryParameters(ref query, values.ToList(), fields.ToList(), combine);
            GetOrderBy(ref query);

            return dbConnection.Query<T>(query.ToString());
        }

        /// <summary>
        /// Get Field Equals
        /// </summary>
        public virtual IEnumerable<T> GetData<T>(String value, Boolean all = false, String field = "id", Boolean lower = true)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append((all ? "" : activeId + " and "));
            GetFieldName(ref query, field, lower); query.Append(" = @Id");
            GetOrderBy(ref query);

            return dbConnection.Query<T>(query.ToString(), new { Id = value });
        }

        /// <summary>
        /// Get Field Equals
        /// </summary>
        public virtual IEnumerable<T> GetData<T>(DateTime value, Boolean all = false, String field = "id")
        {
            StringBuilder query = new StringBuilder();
            query.Append("select ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append((all ? "" : activeId + " and "));
            query.Append(field); query.Append(" = @Id");
            GetOrderBy(ref query);

            return dbConnection.Query<T>(query.ToString(), new { Id = value });
        }

        /// <summary>
        /// Get Field In
        /// </summary>
        public virtual IEnumerable<T> GetData<T>(IEnumerable<Guid> values, Boolean all = false, String field = "id")
        {
            StringBuilder query = new StringBuilder();
            query.Append("select ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append((all ? "" : activeId + " and "));
            query.Append(field); query.Append(" in @Id");
            GetOrderBy(ref query);

            return dbConnection.Query<T>(query.ToString(), new { Id = values.Distinct() });
        }

        /// <summary>
        /// Get Field In
        /// </summary>
        public virtual IEnumerable<T> GetData<T>(IEnumerable<String> values, Boolean all = false, String field = "id", Boolean lower = true)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append((all ? "" : activeId + " and "));
            GetFieldName(ref query, field, lower); query.Append(" in @Id");
            GetOrderBy(ref query);

            return dbConnection.Query<T>(query.ToString(), new { Id = values.Distinct() });
        }

        /// <summary>
        /// Get By Ids Related
        /// </summary>
        public virtual IEnumerable<T> GetData<T>(Guid value, IEnumerable<Guid> values, IEnumerable<String> fields, Boolean combine = false, Boolean all = false)
        {
            if (values == null || fields == null || values.Count().Equals(0) || values.Count() % fields.Count() != 0)
                return null;

            StringBuilder query = new StringBuilder();
            query.Append("select ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append((all ? "" : activeId + " and "));
            query.Append("(id = @Id and ");
            GetQueryParameters(ref query, values.Select(x => x.ToString()).ToList(), fields.ToList(), combine);
            query.Append(")");
            GetOrderBy(ref query);

            return dbConnection.Query<T>(query.ToString(), new { Id = value.ToString() });
        }

        #region Top 1

        public virtual T GetData<T>(int value, Boolean all = false)
        {
            return GetDataTop1<T>(value.ToString(), all, nameId, false);
        }

        public virtual T GetData<T>(Guid value, Boolean all = false)
        {
            return GetDataTop1<T>(value.ToString(), all, nameId, false);
        }

        public virtual T GetDataTop1<T>(String value, Boolean all = false, String field = "id", Boolean lower = true)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select top 1 ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append((all ? "" : activeId + " and "));
            GetFieldName(ref query, field, lower); query.Append(" = @Id");

            return dbConnection.QuerySingleOrDefault<T>(query.ToString(), new { Id = value });
        }

        /// <summary>
        /// Get By Ids
        /// </summary>
        public virtual T GetDataTop1<T>(IEnumerable<String> values, IEnumerable<String> fields = null, Boolean combine = false, Boolean all = false)
        {
            fields = ValidateIdNames(fields, true);
            if (values == null || values.Count().Equals(0) || values.Count() % fields.Count() != 0)
                return (T)(new object());

            StringBuilder query = new StringBuilder();
            query.Append("select top 1 ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append((all ? "" : activeId + " and "));
            GetQueryParameters(ref query, values.ToList(), fields.ToList(), combine);
            GetOrderBy(ref query);

            return dbConnection.QuerySingleOrDefault<T>(query.ToString());
        }

        #endregion

        public virtual IEnumerable<T> GetDataNull<T>(Boolean all = false, String field = "id", Boolean lower = true)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append((all ? "" : activeId + " and "));
            GetFieldName(ref query, field, lower); query.Append(" is null");
            GetOrderBy(ref query);

            return dbConnection.Query<T>(query.ToString());
        }

        public virtual IEnumerable<T> GetDataNot<T>(String value, Boolean all = false, String field = "id", Boolean lower = true)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append((all ? "" : activeId + " and "));
            GetFieldName(ref query, field, lower); query.Append(" <> @Id");
            GetOrderBy(ref query);

            return dbConnection.Query<T>(query.ToString(), new { Id = value });
        }

        public virtual IEnumerable<T> GetDataLike<T>(String value, Boolean all = false, String field = "id", Boolean lower = true)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append((all ? "" : activeId + " and "));
            GetFieldName(ref query, field, lower); query.Append(" like '%@Id%'");
            GetOrderBy(ref query);

            return dbConnection.Query<T>(query.ToString(), new { Id = value });
        }

        #region Exists (select 1)

        public virtual bool ExistsData<T>(String value, int amount = 1, Boolean all = false, String field = "id")
        {
            StringBuilder query = new StringBuilder();
            query.Append("select count(1) from ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append((all ? "" : activeId + " and "));
            query.Append(field); query.Append(" = @Id");

            return dbConnection.ExecuteScalar<int>(query.ToString(), new { Id = value }).Equals(amount);
        }

        public virtual bool ExistsData<T>(IEnumerable<String> values, IEnumerable<String> fields = null, int amount = 1, Boolean combine = false, Boolean all = false)
        {
            fields = ValidateIdNames(fields, true);
            if (values == null || fields == null || values.Count().Equals(0) || values.Count() % fields.Count() != 0)
                return false;

            StringBuilder query = new StringBuilder();
            query.Append("select count(1) from ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append((all ? "" : activeId + " and "));
            GetQueryParameters(ref query, values.ToList(), fields.ToList(), combine);

            return dbConnection.ExecuteScalar<int>(query.ToString()).Equals(amount);
        }

        public virtual bool ExistsData<T>(Guid value, IEnumerable<Guid> values, IEnumerable<String> fields, int amount = 1, Boolean combine = false, Boolean all = false)
        {
            if (values == null || fields == null || values.Count().Equals(0) || fields.Count().Equals(0) || values.Count() % fields.Count() != 0)
                return false;

            StringBuilder query = new StringBuilder();
            query.Append("select count(1) from ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append((all ? "" : activeId + " and "));
            query.Append("(id = @Id or ");
            GetQueryParameters(ref query, values.Select(x => x.ToString()).ToList(), fields.ToList(), combine);
            query.Append(")");

            return dbConnection.ExecuteScalar<int>(query.ToString(), new { Id = value.ToString() }).Equals(amount);
        }

        #endregion

        #region Post

        public virtual bool PostData<T>(T obj, IEnumerable<String> fields = null)
        {
            fields = ValidateIdNames(fields);
            IEnumerable<String> properties = GetProperties(typeof(T), fields);

            StringBuilder query = new StringBuilder();
            query.Append("insert into ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" (");
            query.Append(String.Join(",", properties.Select(x => String.Format("[{0}]", x))));
            query.Append(") values (");
            query.Append(String.Join(",", properties.Select(x => String.Format("@{0}", x))));
            query.Append(")");
            GetQueryHist(ref query, obj, true);

            return dbConnection.Execute(query.ToString(), obj) > 0;
        }

        public virtual bool PostDataMultiple<T>(IEnumerable<T> objs, IEnumerable<String> fields = null)
        {
            fields = ValidateIdNames(fields);
            int result = 0;
            dbConnection.Open();
            using (SqlTransaction tran = dbConnection.BeginTransaction())
            {
                try
                { // multiple operations involving cn and tran here
                    IEnumerable<String> properties = GetProperties(typeof(T), fields);
                    foreach (T item in objs)
                    {
                        StringBuilder query = new StringBuilder();
                        query.Append("insert into ");
                        query.Append(GetTableName(typeof(T)));
                        query.Append(" (");
                        query.Append(String.Join(",", properties.Select(x => String.Format("[{0}]", x))));
                        query.Append(") values (");
                        query.Append(String.Join(",", properties.Select(x => String.Format("@{0}", x))));
                        query.Append(")");
                        GetQueryHist(ref query, item, true);

                        result += dbConnection.Execute(query.ToString(), item, tran);
                    }

                    tran.Commit();
                }
                catch (Exception ex)
                {
                    result = 0;
                    tran.Rollback();
                }
            }

            return result > 0;
        }

        #endregion

        #region Put

        public virtual bool PutData<T>(T obj, IEnumerable<String> fields = null, Boolean combine = false)
        {
            IEnumerable<String> properties = GetProperties(typeof(T), fields);
            fields = ValidateIdNames(fields, true);

            StringBuilder query = new StringBuilder();
            query.Append("update ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" set ");
            query.Append(String.Join(",", properties.Select(x => String.Format("[{0}] = @{1}", x, x))));
            query.Append(" where ");
            GetIdComparer(ref query, fields, combine);
            GetQueryHist(ref query, obj);

            return dbConnection.Execute(query.ToString(), obj) > 0;
        }

        public virtual bool PutDataMultiple<T>(IEnumerable<T> objs, IEnumerable<String> fields = null, Boolean combine = false)
        {
            int result = 0;
            dbConnection.Open();
            using (var tran = dbConnection.BeginTransaction())
            {
                try
                { // multiple operations involving cn and tran here
                    IEnumerable<String> properties = GetProperties(typeof(T), nameId);
                    foreach (T item in objs)
                    {
                        StringBuilder query = new StringBuilder();
                        query.Append("update ");
                        query.Append(GetTableName(typeof(T)));
                        query.Append(" set ");
                        query.Append(String.Join(",", properties.Select(x => String.Format("[{0}] = @{1}", x, x))));
                        query.Append(" where ");
                        GetIdComparer(ref query, fields, combine);
                        GetQueryHist(ref query, item);

                        result += dbConnection.Execute(query.ToString(), item, tran);
                    }

                    tran.Commit();
                }
                catch (Exception ex)
                {
                    result = 0;
                    tran.Rollback();
                }
            }

            return result > 0;
        }

        #endregion

        #region Delete

        public virtual bool DeleteData<T>(T obj)
        {
            IEnumerable<String> properties = GetProperties(typeof(T));

            StringBuilder query = new StringBuilder();
            query.Append("delete ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            GetIdComparer(ref query);
            GetQueryHist(ref query, obj);

            return dbConnection.Execute(query.ToString(), obj) > 0;
        }

        public virtual bool DeleteDataMultiple<T>(IEnumerable<T> obj)
        {
            int result = 0;
            dbConnection.Open();
            using (var tran = dbConnection.BeginTransaction())
            {
                try
                { // multiple operations involving cn and tran here
                    foreach (T item in obj)
                    {
                        StringBuilder query = new StringBuilder();
                        query.Append("delete ");
                        query.Append(GetTableName(typeof(T)));
                        query.Append(" where ");
                        GetIdComparer(ref query);
                        GetQueryHist(ref query, item);

                        result += dbConnection.Execute(query.ToString(), item, tran);
                    }

                    tran.Commit();
                }
                catch
                {
                    result = 0;
                    tran.Rollback();
                }
            }

            return result > 0;
        }

        public virtual bool DeleteData<T>(IEnumerable<Guid> values)
        {
            IEnumerable<String> properties = GetProperties(typeof(T));

            StringBuilder query = new StringBuilder();
            query.Append("delete ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            query.Append(nameId); query.Append(" in @Id");

            return dbConnection.Execute(query.ToString(), new { Id = values.Distinct() }) > 0;
        }

        public virtual bool DeleteData<T>(T obj, IEnumerable<String> fields, Boolean combine = false)
        {
            if (fields == null || fields.Count().Equals(0))
                return false;

            IEnumerable<String> properties = GetProperties(typeof(T));

            StringBuilder query = new StringBuilder();
            query.Append("delete ");
            query.Append(GetTableName(typeof(T)));
            query.Append(" where ");
            GetIdComparer(ref query, fields, combine);
            GetQueryHist(ref query, obj);

            return dbConnection.Execute(query.ToString(), obj) > 0;
        }

        #endregion

        #region Hist

        /// <summary>
        /// Get Hist
        /// </summary>
        public virtual T GetHist<T>(Guid value, Boolean all = false)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select top 1 ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T), true));
            query.Append(" where ");
            query.Append((all ? "" : "active = 1 and "));
            query.Append(histId); query.Append(" = @Id");
            GetOrderBy(ref query);

            return dbConnection.QuerySingleOrDefault<T>(query.ToString(), new { Id = value.ToString() });
        }

        /// <summary>
        /// Get Hist List By Id
        /// </summary>
        public virtual IEnumerable<T> GetByHist<T>(Guid value, Boolean all = true)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T), true));
            query.Append(" where ");
            query.Append((all ? "" : "active = 1 and "));
            query.Append(nameId); query.Append(" = @Id");
            GetOrderBy(ref query);

            return dbConnection.Query<T>(query.ToString(), new { Id = value.ToString() });
        }

        /// <summary>
        /// Get Hist List By Id
        /// </summary>
        public virtual IEnumerable<T> GetByHist<T>(int value, Boolean all = false)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T), true));
            query.Append(" where ");
            query.Append(nameId); query.Append(" = @Id");
            GetOrderBy(ref query);

            return dbConnection.Query<T>(query.ToString(), new { Id = value });
        }

        /// <summary>
        /// Get Hist List By Field
        /// </summary>
        public virtual IEnumerable<T> GetByHist<T>(String value, Boolean all = false, String field = "id", Boolean lower = true)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T), true));
            query.Append(" where ");
            GetFieldName(ref query, field, lower); query.Append(" = @Id");
            GetOrderBy(ref query);

            return dbConnection.Query<T>(query.ToString(), new { Id = value });
        }

        /// <summary>
        /// Get Hist List By Ids
        /// </summary>
        public virtual IEnumerable<T> GetByHist<T>(IEnumerable<Guid> values, IEnumerable<String> fields, Boolean combine = false)
        {
            if (values == null || fields == null || values.Count().Equals(0) || fields.Count().Equals(0) || values.Count() % fields.Count() != 0)
                return null;

            StringBuilder query = new StringBuilder();
            query.Append("select ");
            query.Append(String.Join(",", GetProperties(typeof(T)).Select(x => String.Format("[{0}]", x))));
            query.Append(" from ");
            query.Append(GetTableName(typeof(T), true));
            query.Append(" where ");
            GetQueryParameters(ref query, values.Select(x => x.ToString()).ToList(), fields.ToList(), combine);
            GetOrderBy(ref query);

            return dbConnection.Query<T>(query.ToString());
        }

        #endregion

        #region UnitTest

        public void CleanIntegrationTestData<T>(Boolean all = true)
        {
            StringBuilder query = new StringBuilder();
            query.Append("select ' delete from ");
            query.Append(tablePrefix);
            query.Append("['+t.name+'] where hostaddress = ''UnitTest'' ' from sys.tables t INNER JOIN sys.columns c ON t.OBJECT_ID = c.OBJECT_ID WHERE c.name LIKE '%hostaddress%' ");
            query.Append(all ? "" : " and (t.name = '" + typeof(T) + "' or t.name = '" + typeof(T) + "Hist') ");

            dbConnection.ExecuteScalar(string.Join("", dbConnection.Query<string>(query.ToString())));
        }

        public IEnumerable<T> CheckDatabaseForDeploy<T>()
        {
            StringBuilder query = new StringBuilder();
            query.Append("select T.name as Table_Name, C.name as Column_Name, P.name as Data_Type ");
            query.Append("from sys.objects as T join sys.columns as C on T.object_id = C.object_id join sys.types as P on C.system_type_id = P.system_type_id ");
            query.Append("where T.type_desc = 'USER_TABLE' and SCHEMA_NAME(t.schema_id)='" + tablePrefix + "' order by 1,2,3");

            return dbConnection.Query<T>(query.ToString());
        }

        #endregion
    }
}