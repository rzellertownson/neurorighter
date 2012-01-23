using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace NeuroRighter.SpikeDetection
{
    /// <summary>
    /// Serializer for saving spike detectors to disk.
    /// </summary>
    public class SpikeDetectorSerializer
    {
        /// <summary>
        /// Serialization object for saving a spike sorter for resuse later.
        /// </summary>
        public SpikeDetectorSerializer(){}

        /// <summary>
        /// Serializer for storing a spike detector to disk.
        /// </summary>
        /// <param name="filename">Full path of spike detector file.</param>
        /// <param name="detectorToSerialize"> The SpikeSorter object to be saved.</param>
        public void SerializeObject(string filename, SpikeDetectorParameters detectorToSerialize)
        {
            Stream stream = File.Open(filename, FileMode.Create);
            BinaryFormatter bFormatter = new BinaryFormatter();
            bFormatter.Serialize(stream, detectorToSerialize);
            stream.Close();
        }

        /// <summary>
        /// DeSerializer for loading a SpikeDetector object.
        /// </summary>
        /// <param name="filename">Full path of spike detector file.</param>
        public SpikeDetectorParameters DeSerializeObject(string filename)
        {
            SpikeDetectorParameters detectorToSerialize;
            Stream stream = File.Open(filename, FileMode.Open);
            BinaryFormatter bFormatter = new BinaryFormatter();
            detectorToSerialize = (SpikeDetectorParameters)bFormatter.Deserialize(stream);
            stream.Close();
            return detectorToSerialize;
        }
    }
}