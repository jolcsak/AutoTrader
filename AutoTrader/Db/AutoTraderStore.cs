using RethinkDb.Driver;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Extras.Dao;
using System;

namespace AutoTrader.Db
{
    public class AutoTraderStore<T, TD> : RethinkDao<T, Guid>
        where T: IDocument<Guid>
        where TD : RethinkDao<T, Guid>
    {
        public const string DatabaseName = "AutoTraderStore";

        protected static Store DbStore => Store.Instance;

        public AutoTraderStore() : base(Store.Instance.Con, DatabaseName, typeof(TD).Name)
        {
        }

        protected RethinkDb.Driver.Ast.Db Db => R.Db(DbName);
        protected Table DbTable => Db.Table(this.GetType().Name);

    }
}
