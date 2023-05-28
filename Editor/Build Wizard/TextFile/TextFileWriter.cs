/*
 * This document is the property of Oversight Technologies Ltd that reserves its rights document and to
 * the data / invention / content herein described.This document, including the fact of its existence, is not to be
 * disclosed, in whole or in part, to any other party and it shall not be duplicated, used, or copied in any
 * form, without the express prior written permission of Oversight authorized person. Acceptance of this document
 * will be construed as acceptance of the foregoing conditions.
 */

using System.IO;
using System.Text;

namespace Editor.Build_Wizard.TextFile {
	public class TextFileWriter {
		public void WriteToTextFileAt(string path, string content)
		{
			using var fileStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write);
			var info = new UTF8Encoding(true).GetBytes(content);
			fileStream.Write(info, 0, info.Length);
		}
	}
}