using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Protobot.Builds.Windows {
    public class WindowsBuildHandler : MonoBehaviour, IBuildHandler {
        public void Save(BuildData buildData) {
            BinaryFormatter bf = new BinaryFormatter();

            string fileLocation = GetFileLocation(buildData);

            Directory.CreateDirectory(WindowsSavingConfig.saveDirectoryPath);
            FileStream file = File.Create(fileLocation);
            bf.Serialize(file, buildData);
            file.Close();
        }

        public void Delete(BuildData buildData) {
            var fileLocation = GetFileLocation(buildData);
            File.Delete(fileLocation);
        }

        public string GetFileLocation(BuildData buildData) {
            return Path.Combine(
                WindowsSavingConfig.saveDirectoryPath,
                buildData.fileName + WindowsSavingConfig.saveFileType
            );
        }

        public DateTime GetExactWriteTime(BuildData buildData) {
            var fileLocation = GetFileLocation(buildData);
            return File.GetLastWriteTime(fileLocation);
        }
    }
}
