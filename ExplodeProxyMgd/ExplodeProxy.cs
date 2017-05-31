using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;

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

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                try
                {
                    // Request for objects to be selected in the drawing area
                    PromptSelectionResult acSSPrompt = doc.Editor.GetSelection();

                    // If the prompt status is OK, objects were selected
                    if (acSSPrompt.Status == PromptStatus.OK)
                    {
                        SelectionSet acSSet = acSSPrompt.Value;

                        // Step through the objects in the selection set
                        foreach (SelectedObject acSSObj in acSSet)
                        {
                            // Check to make sure a valid SelectedObject object was returned
                            if (acSSObj != null)
                            {
                                // Open the selected object for write
                                DBObjectCollection objs = new DBObjectCollection();
                                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                                Entity entx = tr.GetObject(acSSObj.ObjectId, OpenMode.ForWrite) as Entity;
                                entx.Explode(objs);

                                string blkName = entx.Handle.ToString();

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

                                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite );

                                    BlockReference br =
                                      new BlockReference(Point3d.Origin, btrId);

                                    ms.AppendEntity(br);
                                    tr.AddNewlyCreatedDBObject(br, true);


                                }

                                tr.Commit();
                            }
                        }
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    tr.Abort();
                    ed.WriteMessage(ex.Message);
                }
            }
        }
    }
}
