using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#region Autodesk
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
#endregion

namespace ExplodeProxyMgd
{
    class cEntity
    {
        private Entity _EntProxy;


        public Entity EntProxy
        {
            get
            {
                return _EntProxy;
            }

            set
            {
                _EntProxy = value;
            }
        }

        public SelectionSet Selection(Document doc, Transaction tr)
        {
            try
            {
                PromptSelectionResult acSSPrompt = doc.Editor.GetSelection();

                if (acSSPrompt.Status == PromptStatus.OK)
                {
                    SelectionSet acSSet = acSSPrompt.Value;
                    return acSSet;
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {

            }
            return null;
        }

        public void CreateBlock()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                DBObjectCollection objs = new DBObjectCollection();
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                EntProxy.Explode(objs);

                string blkName = EntProxy.Handle.ToString();

                if (bt.Has(blkName) == false)
                {
                    BlockTableRecord btr = new BlockTableRecord();
                    btr.Name = blkName;

                    bt.UpgradeOpen();
                    ObjectId btrId = bt.Add(btr);
                    tr.AddNewlyCreatedDBObject(btr, true);

                    foreach (DBObject obj in objs)
                    {
                        Entity ent = (Entity)obj;
                        btr.AppendEntity(ent);
                        tr.AddNewlyCreatedDBObject(ent, true);
                    }

                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    BlockReference br =
                      new BlockReference(Point3d.Origin, btrId);

                    ms.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);
                }
                tr.Commit();
            }
        }

        public List<ProxyEntity> GetProxies()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            List<ProxyEntity> result = new List<ProxyEntity>();

            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                    foreach (ObjectId entId in btr)
                    {
                        if (entId.ObjectClass.Name == "AcDbZombieEntity")
                        {
                            ProxyEntity ent = (ProxyEntity)tr.GetObject(entId, OpenMode.ForRead);
                            result.Add(ent);
                        }
                    }
                }
                tr.Commit();

                return result;
            }
            return null;
        }

        public void RemoveProxy()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            ObjectId id = EntProxy.ObjectId;

            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject( db.BlockTableId, OpenMode.ForRead);

                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                    foreach (ObjectId entId in btr)
                    {
                        if (entId.ObjectClass.Name == "AcDbZombieEntity" && entId == id)
                        {
                            ProxyEntity ent = (ProxyEntity)tr.GetObject(entId, OpenMode.ForRead);

                            ent.UpgradeOpen();

                            using (DBObject newEnt = new Line())
                            {
                                ent.HandOverTo(newEnt, false, false);
                                newEnt.Erase();
                            }
                        }
                    }
                }

                tr.Commit();
            }
        }
    }
}
