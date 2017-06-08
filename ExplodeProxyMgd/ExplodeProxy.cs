using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#region Autodesk
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
#endregion

[assembly: CommandClass(typeof(ExplodeProxyMgd.ExplodeProxy))]

namespace ExplodeProxyMgd
{
    public class ExplodeProxy
    {

        private static ObjectIdCollection ids = new ObjectIdCollection();



        [CommandMethod("proxy-explode-to-block")]
        public void ProxyExplodeToBlock()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            int incr = 0;

            try
            {
                cEntity oEnt = new cEntity();
                List<ProxyEntity> proxies = oEnt.GetProxies();
                foreach (ProxyEntity e in proxies)
                {
                    oEnt.EntProxy = e as Entity;
                    oEnt.CreateBlock();
                    oEnt.RemoveProxy();

                    incr++;

                    //e.Clone();
                }

                ed.WriteMessage(string.Format("\n" + incr + " Proxies in Blocks converted."));
            }
            catch(System.Exception ex) { }

            //using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            //{
            //   SelectionSet acSSet =  oEnt.Selection(doc, tr);

            //    foreach (SelectedObject acSSObj in acSSet)
            //    {
            //        if (acSSObj != null)
            //        {

            //        }
        }

        [CommandMethod("RemoveProxiesFromBlocks", "RemoveProxiesFromBlocks", CommandFlags.Modal)]
        public void RemoveProxies()
        {
            Database db = HostApplicationServices.WorkingDatabase;

            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject( db.BlockTableId, OpenMode.ForRead);

                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                    foreach (ObjectId entId in btr)
                    {
                        if (entId.ObjectClass.Name == "AcDbZombieEntity")
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
