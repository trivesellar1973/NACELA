using System;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal static class B2BodyOps
    {
        // SolidWorks swBodyOperationType_e: SWBODYADD = 15901.
        // Se crea primero cada loft como cuerpo independiente y luego se suma.
        // Esto es mas robusto que exigir Merge result durante la creacion del loft.
        public static Feature AddBodies(IModelDoc2 doc, Body2 mainBody, Body2 addedBody, string name)
        {
            if (mainBody == null) throw new ArgumentNullException("mainBody");
            if (addedBody == null) throw new ArgumentNullException("addedBody");

            ISelectionMgr selectionManager = (ISelectionMgr)doc.SelectionManager;
            SelectData mainData = (SelectData)selectionManager.CreateSelectData();
            SelectData addedData = (SelectData)selectionManager.CreateSelectData();
            mainData.Mark = 1;
            addedData.Mark = 2;

            doc.ClearSelection2(true);
            if (!mainBody.Select2(false, mainData))
                throw new InvalidOperationException("No se selecciono el cuerpo principal para " + name);
            if (!addedBody.Select2(true, addedData))
                throw new InvalidOperationException("No se selecciono el cuerpo agregado para " + name);

            Feature combine = doc.FeatureManager.InsertCombineFeature(15901, null, null);
            doc.ClearSelection2(true);
            if (combine == null)
                throw new InvalidOperationException("Fallo la union booleana de " + name + ". Los cuerpos deben intersectarse.");

            combine.Name = name;
            doc.EditRebuild3();
            return combine;
        }
    }
}
