/*
 * This document is the property of Oversight Technologies Ltd that reserves its rights document and to
 * the data / invention / content herein described.This document, including the fact of its existence, is not to be
 * disclosed, in whole or in part, to any other party and it shall not be duplicated, used, or copied in any
 * form, without the express prior written permission of Oversight authorized person. Acceptance of this document
 * will be construed as acceptance of the foregoing conditions.
 */

using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Build_Wizard.Wizard
{
    public class BuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!BuildWizard.IsBuilding)
            {
                throw new BuildFailedException("You are using the wrong build tool. Use the Build Wizard instead!");
            }
        }
    }
}
