﻿using System;
using NewLife.Linq;
using NewLife.Reflection;

namespace XCode.Sync
{
    /// <summary>同步架构主方接口</summary>
    public interface ISyncMaster
    {
        #region 方法
        /// <summary>检查在指定时间后更新过的数据</summary>
        /// <param name="last"></param>
        /// <param name="start"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        ISyncMasterEntity[] GetAllUpdated(DateTime last, Int32 start, Int32 max);

        /// <summary>提交新增数据</summary>
        /// <param name="list"></param>
        /// <returns>返回新增成功后的数据，包括自增字段</returns>
        ISyncMasterEntity[] Insert(ISyncMasterEntity[] list);

        /// <summary>更新数据</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        ISyncMasterEntity Update(ISyncMasterEntity entity);

        /// <summary>根据主键数组删除数据</summary>
        /// <param name="keys"></param>
        /// <returns>是否删除成功</returns>
        Boolean[] Delete(Object[] keys);

        /// <summary>根据主键数组检查数据是否仍然存在</summary>
        /// <param name="keys"></param>
        /// <returns>是否存在</returns>
        Boolean[] CheckExists(Object[] keys);

        /// <summary>创建一个空白实体</summary>
        /// <returns></returns>
        ISyncMasterEntity Create();

        /// <summary>获取要同步的字段名</summary>
        /// <returns></returns>
        String[] GetNames();
        #endregion
    }

    /// <summary>同步框架主方实体接口，由主方实体类实现</summary>
    public interface ISyncMasterEntity : IIndexAccessor
    {
        #region 属性
        /// <summary>唯一标识数据的键值</summary>
        Object Key { get; }

        /// <summary>最后修改时间。包括修改同步状态为假删除</summary>
        DateTime LastUpdate { get; }
        #endregion
    }

    /// <summary>同步架构主方</summary>
    public class SyncMaster : ISyncMaster
    {
        #region 属性
        private IEntityOperate _Facotry;
        /// <summary>工厂</summary>
        public IEntityOperate Facotry { get { return _Facotry; } set { _Facotry = value; } }

        /// <summary>主键名</summary>
        protected virtual String KeyName { get { return Facotry.Unique.Name; } }

        /// <summary>最后更新字段名</summary>
        protected virtual String LastUpdateName { get { return "LastUpdate"; } }
        #endregion

        #region 方法
        /// <summary>检查在指定时间后更新过的数据</summary>
        /// <param name="last"></param>
        /// <param name="start"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public virtual ISyncMasterEntity[] GetAllUpdated(DateTime last, Int32 start, Int32 max)
        {
            var list = Facotry.FindAll(Facotry.MakeCondition(LastUpdateName, last, ">"), null, null, start, max);
            if (list == null || list.Count < 1) return null;

            // 如果实体类实现了该接口，则返回
            if (Facotry.Default is ISyncMasterEntity)
            {
                var rs = new ISyncMasterEntity[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    rs[i] = list[i] as ISyncMasterEntity;
                }
                return rs;
            }
            // 否则采用内置实现
            else
            {
                var rs = new ISyncMasterEntity[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    rs[i] = new SyncMasterEntity(this, list[i]);
                }
                return rs;
            }
        }

        /// <summary>提交新增数据</summary>
        /// <param name="list"></param>
        /// <returns>返回新增成功后的数据，包括自增字段</returns>
        public virtual ISyncMasterEntity[] Insert(ISyncMasterEntity[] list)
        {
            if (list == null) return null;
            if (list.Length < 1) return new ISyncMasterEntity[0];

            for (int i = 0; i < list.Length; i++)
            {
                var item = list[i];
                if (item != null)
                {
                    var entity = item as IEntity;
                    if (entity == null && item is SyncMasterEntity) entity = (item as SyncMasterEntity).Entity;

                    if (entity != null)
                        entity.Insert();
                    else
                        list[i] = null;
                }
                else
                    list[i] = null;
            }

            return list;
        }

        /// <summary>更新数据</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual ISyncMasterEntity Update(ISyncMasterEntity entity)
        {
            if (entity == null) return null;

            var e = entity as IEntity;
            if (e == null && entity is SyncMasterEntity) e = (entity as SyncMasterEntity).Entity;

            if (e == null) return null;

            e.Update();

            return entity;
        }

        /// <summary>根据主键数组删除数据</summary>
        /// <param name="keys"></param>
        /// <returns>是否删除成功</returns>
        public virtual Boolean[] Delete(Object[] keys)
        {
            if (keys == null) return null;
            if (keys.Length < 1) return new Boolean[0];

            var rs = new Boolean[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                var entity = Facotry.FindByKey(keys[i]);
                if (entity != null)
                {
                    entity.Delete();
                    rs[i] = true;
                }
            }

            return rs;
        }

        /// <summary>根据主键数组检查数据是否仍然存在</summary>
        /// <param name="keys"></param>
        /// <returns>是否存在</returns>
        public virtual Boolean[] CheckExists(Object[] keys)
        {
            if (keys == null) return null;
            if (keys.Length < 1) return new Boolean[0];

            var rs = new Boolean[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                var entity = Facotry.FindByKey(keys[i]);
                if (entity != null) rs[i] = true;
            }

            return rs;
        }

        /// <summary>创建一个空白实体</summary>
        /// <returns></returns>
        public virtual ISyncMasterEntity Create()
        {
            var entity = Facotry.Create();
            if (entity is ISyncMasterEntity)
                return entity as ISyncMasterEntity;
            else
                return new SyncMasterEntity(this, entity);
        }

        /// <summary>获取要同步的字段名</summary>
        /// <returns></returns>
        public virtual String[] GetNames() { return Facotry.FieldNames.ToArray(); }
        #endregion

        #region 实体
        class SyncMasterEntity : ISyncMasterEntity
        {
            private SyncMaster _Host;
            /// <summary>宿主</summary>
            public SyncMaster Host { get { return _Host; } set { _Host = value; } }

            private IEntity _Entity;
            /// <summary>实体</summary>
            public IEntity Entity { get { return _Entity; } set { _Entity = value; } }

            public Object Key { get { return Entity[Host.KeyName]; } }

            public DateTime LastUpdate { get { return (DateTime)Entity[Host.LastUpdateName]; } }

            public Object this[String name] { get { return Entity[name]; } set { Entity[name] = value; } }

            public SyncMasterEntity(SyncMaster host, IEntity entity) { Host = host; Entity = entity; }
        }
        #endregion
    }
}