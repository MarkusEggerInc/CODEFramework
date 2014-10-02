using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using CODE.Framework.Core.Utilities.Extensions;

namespace CODE.Framework.Core.Utilities
{
	/// <summary>
	/// ZIP File handling class
	/// </summary>
	/// <example>
	/// // Open existing ZIP file
	/// var zip = ZipFile.Read(@"c:\test.zip");
	/// zip.ExtractAll(@"c:\ExtractFolder");
	/// 
	/// // Create new ZIP file
	/// var zip = new ZipFile("MyFile.zip");
	/// zip.AddFile(@"c:\Markus.jpg");
	/// zip.AddBytes("This is a test file".ToByteArraySafe(), "ReadMe.txt");
	/// zip.Save();
	/// </example>
	public class ZipFile : IEnumerable<ZipEntry>, IDisposable
	{
		/// <summary>
		/// Name of the ZIP File
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; internal set; }

		/// <summary>Indicates whether the volume should be trimmed from fully qualified paths</summary>
		/// <value><c>true</c> if [trim volume from fully qualified paths]; otherwise, <c>false</c>.</value>
		/// <remarks>
		/// when this is set, we trim the volume (eg C:) off any fully-qualified pathname, 
		/// before writing the ZipEntry into the ZipFile. 
		/// We default this to true.  This allows Windows Explorer to read the zip archives properly. 
		/// </remarks>
		public bool TrimVolumeFromFullyQualifiedPaths { get; set; }

		/// <summary>
		/// Read stream used internally by this class
		/// </summary>
		/// <value>The read stream.</value>
		private Stream ReadStream
		{
			get { return _readStream ?? (_readStream = File.OpenRead(Name)); }
		}
		/// <summary>
		/// Internal field for the read stream
		/// </summary>
		private Stream _readStream;

		/// <summary>
		/// Write stream used internally by this class
		/// </summary>
		/// <value>The write stream.</value>
		private FileStream WriteStream
		{
			get { return _writeStream ?? (_writeStream = new FileStream(Name, FileMode.CreateNew)); }
		}
		/// <summary>
		/// Internal field for the write stream
		/// </summary>
		private FileStream _writeStream;

		/// <summary>
		/// Initializes the default values of properties inside this class
		/// </summary>
		private void InitializeDefaultValues()
		{
			TrimVolumeFromFullyQualifiedPaths = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ZipFile"/> class.
		/// </summary>
		private ZipFile()
		{
			InitializeDefaultValues();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ZipFile"/> class.
		/// </summary>
		/// <param name="newZipFileName">Name of the new ZIP file.</param>
		/// <example>
		/// var newZip = new ZipFile("MyNewZipFile.zip");
		/// </example>
		public ZipFile(string newZipFileName)
		{
			InitializeDefaultValues();

			// create a new zipfile
			Name = newZipFileName;
		    if (File.Exists(Name))
		        File.Delete(Name);
		    _entries = new List<ZipEntry>();
		}

		/// <summary>
		/// Adds a file or folder to the ZIP archive
		/// </summary>
		/// <param name="fileOrDirectoryName">Name of the file or directory.</param>
		/// <example>
		/// var newZip = new ZipFile("MyNewZipFile.zip");
		/// newZip.AddItem("C:\DirectoryToZip");
		/// newZip.AddItem("C:\Folder\Test.exe");
		/// </example>
		public void AddItem(string fileOrDirectoryName)
		{
		    if (File.Exists(fileOrDirectoryName))
		        AddFile(fileOrDirectoryName);
		    else if (Directory.Exists(fileOrDirectoryName))
		        AddDirectory(fileOrDirectoryName);
		    else
		        throw new Exception(String.Format("That file or directory ({0}) does not exist!", fileOrDirectoryName));
		}

		/// <summary>
		/// Adds raw data to the ZIP archive as a file.
		/// </summary>
		/// <param name="bytesToCompress">Data/ file bytes</param>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="fileWriteTime">The file write time.</param>
		/// <returns>ZIP Entry</returns>
		/// <example>
		/// var newZip = new ZipFile("MyNewZipFile.zip");
		/// newZip.AddBytes("Hello World".ToByteArraySafe(), "Test.txt", DateTime.Now);
		/// </example>
		public ZipEntry AddBytes(byte[] bytesToCompress, string fileName, DateTime fileWriteTime)
		{
			var zipEntry = ZipEntry.Create(bytesToCompress, fileName, fileWriteTime);
			_entries.Add(zipEntry);
			return zipEntry;
		}

		/// <summary>
		/// Adds raw data to the ZIP archive as a file.
		/// </summary>
		/// <param name="bytesToCompress">Data/ file bytes</param>
		/// <param name="fileName">Name of the file.</param>
		/// <returns>ZIP Entry</returns>
		/// <example>
		/// var newZip = new ZipFile("MyNewZipFile.zip");
		/// newZip.AddBytes("Hello World".ToByteArraySafe(), "Test.txt");
		/// </example>
		/// <remarks>Current date time is assumed as the file date.</remarks>
		public ZipEntry AddBytes(byte[] bytesToCompress, string fileName)
		{
			return AddBytes(bytesToCompress, fileName, DateTime.Now);
		}

		/// <summary>
		/// Adds the file to the ZIP archive.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns></returns>
		/// <example>
		/// var newZip = new ZipFile("MyNewZipFile.zip");
		/// newZip.AddFile("C:\Folder\Test.exe");
		/// </example>
		public ZipEntry AddFile(string fileName)
		{
			var zipEntry = ZipEntry.Create(fileName);
			_entries.Add(zipEntry);
			return zipEntry;
		}

		/// <summary>
		/// Adds a whole Directory/Folder to the ZIP archive
		/// </summary>
		/// <param name="directory">The directory.</param>
		/// <example>
		/// var newZip = new ZipFile("MyNewZipFile.zip");
		/// newZip.AddDirectory("C:\Folder");
		/// </example>
		public void AddDirectory(string directory)
		{
			string[] fileNames = Directory.GetFiles(directory);
		    foreach (var fileName in fileNames)
		        AddFile(fileName);

		    string[] directoryNames = Directory.GetDirectories(directory);
		    foreach (string dir in directoryNames)
		        AddDirectory(dir);
		}

		/// <summary>
		/// Saves the ZIP archive to a stream
		/// </summary>
		/// <returns>Stream</returns>
		/// <example>
		/// var newZip = new ZipFile("MyNewZipFile.zip");
		/// newZip.AddDirectory("C:\Folder");
		/// Stream result = newZip.SaveToStream();
		/// </example>
		public Stream SaveToStream()
		{
			var stream = new MemoryStream();
		    foreach (var entry in _entries)
		        entry.Write(stream);
		    return stream;
		}

		/// <summary>
		/// Saves the ZIP file
		/// </summary>
		/// <example>
		/// var newZip = new ZipFile("MyNewZipFile.zip");
		/// newZip.AddDirectory("C:\Folder");
		/// newZip.Save();
		/// </example>
		public void Save()
		{
			// an entry for each file
		    foreach (var entry in _entries)
		        entry.Write(WriteStream);

		    WriteCentralDirectoryStructure();
			WriteStream.Close();
			WriteStream.Dispose();
			_writeStream = null;
		}

		/// <summary>
		/// Writes the central directory structure.
		/// </summary>
		private void WriteCentralDirectoryStructure()
		{
			// the central directory structure
			long start = WriteStream.Length;
		    foreach (var entry in _entries)
		        entry.WriteCentralDirectoryEntry(WriteStream);
		    long finish = WriteStream.Length;

			// now, the footer
			WriteCentralDirectoryFooter(start, finish);
		}

		/// <summary>
		/// Writes the central directory footer.
		/// </summary>
		/// <param name="startOfCentralDirectory">The start of central directory.</param>
		/// <param name="endOfCentralDirectory">The end of central directory.</param>
		private void WriteCentralDirectoryFooter(long startOfCentralDirectory, long endOfCentralDirectory)
		{
			var bytes = new byte[1024];
			var i = 0;
			// signature
			const int endOfCentralDirectorySignature = 0x06054b50;
			bytes[i++] = endOfCentralDirectorySignature & 0x000000FF;
			bytes[i++] = (endOfCentralDirectorySignature & 0x0000FF00) >> 8;
			bytes[i++] = (endOfCentralDirectorySignature & 0x00FF0000) >> 16;
			bytes[i++] = (byte)((endOfCentralDirectorySignature & 0xFF000000) >> 24);

			// number of this disk
			bytes[i++] = 0;
			bytes[i++] = 0;

			// number of the disk with the start of the central directory
			bytes[i++] = 0;
			bytes[i++] = 0;

			// total number of entries in the central dir on this disk
			bytes[i++] = (byte)(_entries.Count & 0x00FF);
			bytes[i++] = (byte)((_entries.Count & 0xFF00) >> 8);

			// total number of entries in the central directory
			bytes[i++] = (byte)(_entries.Count & 0x00FF);
			bytes[i++] = (byte)((_entries.Count & 0xFF00) >> 8);

			// size of the central directory
			var sizeOfCentralDirectory = (Int32)(endOfCentralDirectory - startOfCentralDirectory);
			bytes[i++] = (byte)(sizeOfCentralDirectory & 0x000000FF);
			bytes[i++] = (byte)((sizeOfCentralDirectory & 0x0000FF00) >> 8);
			bytes[i++] = (byte)((sizeOfCentralDirectory & 0x00FF0000) >> 16);
			bytes[i++] = (byte)((sizeOfCentralDirectory & 0xFF000000) >> 24);

			// offset of the start of the central directory 
			var startOffset = (Int32)startOfCentralDirectory;  // cast down from Long
			bytes[i++] = (byte)(startOffset & 0x000000FF);
			bytes[i++] = (byte)((startOffset & 0x0000FF00) >> 8);
			bytes[i++] = (byte)((startOffset & 0x00FF0000) >> 16);
			bytes[i++] = (byte)((startOffset & 0xFF000000) >> 24);

			// zip comment length
			bytes[i++] = 0;
			bytes[i++] = 0;

			WriteStream.Write(bytes, 0, i);
		}

		/// <summary>
		/// This will throw if the zipfile does not exist.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns>ZIP File</returns>
		/// <example>
		/// var archive = ZipFile.Read("c:\Downloads\Test.zip");
		/// </example>
		public static ZipFile Read(string fileName)
		{
			var file = new ZipFile {Name = fileName, _entries = new List<ZipEntry>()};
			ZipEntry entry;
			while ((entry = ZipEntry.Read(file.ReadStream)) != null)
				file._entries.Add(entry);

			// read the zipfile's central directory structure here.
			file._directoryEntries = new List<ZipDirEntry>();

			ZipDirEntry directoryEntry;
			while ((directoryEntry = ZipDirEntry.Read(file.ReadStream)) != null)
				file._directoryEntries.Add(directoryEntry);

			return file;
		}

		/// <summary>
		/// Reads a ZIP File from in-memory bytes
		/// </summary>
		/// <param name="zipBytes">The zip bytes.</param>
		/// <param name="fileName">Name of the file.</param>
		/// <returns></returns>
		/// <example>
		/// var zipFile = ZipFile.Read(data, "Test.zip");
		/// </example>
		public static ZipFile Read(byte[] zipBytes, string fileName)
		{
			var file = new ZipFile {Name = fileName, _entries = new List<ZipEntry>()};
			ZipEntry entry;
			var stream = StreamHelper.FromArray(zipBytes);
			while ((entry = ZipEntry.Read(stream)) != null)
				file._entries.Add(entry);

			// read the zipfile's central directory structure here.
			file._directoryEntries = new List<ZipDirEntry>();

			ZipDirEntry directoryEntry;
			while ((directoryEntry = ZipDirEntry.Read(stream)) != null)
				file._directoryEntries.Add(directoryEntry);

			return file;
		}

		/// <summary>
		/// Reads a ZIP File from in-memory bytes
		/// </summary>
		/// <param name="zipBytes">The zip bytes.</param>
		/// <returns></returns>
		/// <example>
		/// var zipFile = ZipFile.Read(data);
		/// </example>
		public static ZipFile Read(byte[] zipBytes)
		{
			return Read(zipBytes, string.Empty);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<ZipEntry> GetEnumerator()
		{
			return ((IEnumerable<ZipEntry>)_entries).GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
		/// </returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Extracts all contents of the ZIP File to the specified path
		/// </summary>
		/// <param name="path">The path.</param>
		/// <example>
		/// var archive = ZipFile.Read("c:\Downloads\Test.zip");
		/// archive.ExtractAll("c:\Downloads\Extract");
		/// </example>
		public void ExtractAll(string path)
		{
			foreach (ZipEntry entry in _entries)
			{
				entry.Extract(path);
			}
		}


		/// <summary>
		/// Extracts the specified file from the ZIP archive
		/// </summary>
		/// <param name="fileName">Name of the file within the archive.</param>
		/// <example>
		/// var zipFile = ZipFile.Read("Photos.zip");
		/// zipFile.Extract("Markus.jpg");
		/// </example>
		public void Extract(string fileName)
		{
			this[fileName].Extract();
		}


		/// <summary>
		/// Extracts the specified file name into the extract stream.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="extractStream">The extract stream.</param>
		/// var zipFile = ZipFile.Read("Photos.zip");
		/// var stream = new MemoryStream();
		/// zipFile.Extract("Markus.jpg", stream);
		public void Extract(string fileName, Stream extractStream)
		{
			this[fileName].Extract(extractStream);
		}


		/// <summary>
		/// Gets the ZIP file corresponding with the provided file name
		/// </summary>
		/// <value></value>
		/// <example>
		/// var zipFile = ZipFile.Read("Photos.zip");
		/// var photo = zipFile["Markus.jpg"];
		/// </example>
		public ZipEntry this[string fileName]
		{
			get { return _entries.FirstOrDefault(entry => entry.FileName == fileName); }
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="ZipFile"/> is reclaimed by garbage collection.
		/// </summary>
		~ZipFile()
		{
			// call Dispose with false.  Since we're in the
			// destructor call, the managed resources will be
			// disposed of anyways.
			Dispose(false);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			// dispose of the managed and unmanaged resources
			Dispose(true);

			// tell the GC that the Finalize process no longer needs
			// to be run for this object.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposeManagedResources"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposeManagedResources)
		{
			if (!_disposed)
			{
				if (disposeManagedResources)
				{
					// dispose managed resources
					if (_readStream != null)
					{
						_readStream.Dispose();
						_readStream = null;
					}
					if (_writeStream != null)
					{
						_writeStream.Dispose();
						_writeStream = null;
					}
				}
				_disposed = true;
			}
		}

		/// <summary>
		/// Indicator whether or not the dispose ran (used internally)
		/// </summary>
		private bool _disposed;
		/// <summary>
		/// List of ZIP entries (used internally)
		/// </summary>
		private List<ZipEntry> _entries;
		/// <summary>
		/// List of Directory entries (used internally)
		/// </summary>
		private List<ZipDirEntry> _directoryEntries;
	}

	/// <summary>
	/// Represents a single entry (file) within a ZIP archive
	/// </summary>
	public class ZipEntry
	{
		/// <summary>
		/// ZIP Entry Signature
		/// </summary>
		private const int ZipEntrySignature = 0x04034b50;
		/// <summary>
		/// ZIP Entry Data Descriptor Signature
		/// </summary>
		private const int ZipEntryDataDescriptorSignature = 0x08074b50;

		/// <summary>
		/// Gets or sets the last modified date.
		/// </summary>
		/// <value>The last modified.</value>
		public DateTime LastModified { get; private set; }
		/// <summary>
		/// Gets or sets the name of the file.
		/// </summary>
		/// <value>The name of the file.</value>
		public string FileName { get; set; }

		/// <summary>
		/// Gets or sets the bytes to compres.
		/// </summary>
		/// <value>The bytes to compres.</value>
		/// <remarks>
		/// If this is set, this array is used, rather than a file that needs to be opened
		/// </remarks>
		private byte[] BytesToCompres { get; set; }
		/// <summary>
		/// Gets or sets the directory name override.
		/// </summary>
		/// <value>The directory name override.</value>
		public string DirectoryNameOverride { get; set; }

		/// <summary>
		/// Gets the name of the compressed file.
		/// </summary>
		/// <value>The name of the compressed file.</value>
		private string CompressedFileName
		{
			get
			{
				string fileName = FileName;
				if (!string.IsNullOrEmpty(DirectoryNameOverride))
				{
					string oldPath = FileName;
				    if (oldPath.IndexOf(@"\") > -1)
				    {
				        oldPath = StringHelper.JustPath(FileName);
				        fileName = StringHelper.AddBS(DirectoryNameOverride) + fileName.Substring(oldPath.Length + 1);
				    }
				    else
				        fileName = StringHelper.AddBS(DirectoryNameOverride) + fileName;
				}
			    fileName = fileName.Replace("\\", "/"); // ZIP files use forward slashes
				return fileName;
			}
		}
		/// <summary>
		/// Gets or sets the version needed.
		/// </summary>
		/// <value>The version needed.</value>
		public Int16 VersionNeeded { get; private set; }
		/// <summary>
		/// Gets or sets the bit field.
		/// </summary>
		/// <value>The bit field.</value>
		public Int16 BitField { get; private set; }
		/// <summary>
		/// Gets or sets the compression method.
		/// </summary>
		/// <value>The compression method.</value>
		public Int16 CompressionMethod { get; private set; }
		/// <summary>
		/// Gets or sets the size of the compressed.
		/// </summary>
		/// <value>The size of the compressed.</value>
		public Int32 CompressedSize { get; private set; }
		/// <summary>
		/// Gets or sets the size of the uncompressed.
		/// </summary>
		/// <value>The size of the uncompressed.</value>
		public Int32 UncompressedSize { get; private set; }

		/// <summary>
		/// Gets the compression ratio.
		/// </summary>
		/// <value>The compression ratio.</value>
		public Double CompressionRatio
		{
			get
			{
				return 100 * (1.0 - (1.0 * CompressedSize) / (1.0 * UncompressedSize));
			}
		}

		private Int32 _lastModDateTime;
		private Int32 _crc32;
		private byte[] _extra;
		private byte[] _fileData;
		private MemoryStream _underlyingMemoryStream;
		private DeflateStream _compressedStream;
		private DeflateStream CompressedStream
		{
			get
			{
				if (_compressedStream == null)
				{
					_underlyingMemoryStream = new MemoryStream();
					_compressedStream = new DeflateStream(_underlyingMemoryStream, CompressionMode.Compress, true);
				}
				return _compressedStream;
			}
		}
		internal byte[] Header { get; private set; }
		private int _relativeOffsetOfHeader;

		private static bool ReadHeader(Stream stream, ZipEntry entry)
		{
			int signature = SharedZipFunctionality.ReadSignature(stream);

			// return null if this is not a local file header signature
			if (SignatureIsNotValid(signature))
			{
				stream.Seek(-4, SeekOrigin.Current);
				return false;
			}

			var block = new byte[26];
			var bytesRead = stream.Read(block, 0, block.Length);
			if (bytesRead != block.Length) return false;

			var counter = 0;
			entry.VersionNeeded = (short)(block[counter++] + block[counter++] * 256);
			entry.BitField = (short)(block[counter++] + block[counter++] * 256);
			entry.CompressionMethod = (short)(block[counter++] + block[counter++] * 256);
			entry._lastModDateTime = block[counter++] + block[counter++] * 256 + block[counter++] * 256 * 256 + block[counter++] * 256 * 256 * 256;

			// the PKZIP spec says that if bit 3 is set (0x0008), then the CRC, Compressed size, and uncompressed size
			// come directly after the file data.  The only way to find it is to scan the zip archive for the signature of 
			// the Data Descriptor, and presume that that signature does not appear in the (compressed) data of the compressed file.  

			if ((entry.BitField & 0x0008) != 0x0008)
			{
				entry._crc32 = block[counter++] + block[counter++] * 256 + block[counter++] * 256 * 256 + block[counter++] * 256 * 256 * 256;
				entry.CompressedSize = block[counter++] + block[counter++] * 256 + block[counter++] * 256 * 256 + block[counter++] * 256 * 256 * 256;
				entry.UncompressedSize = block[counter++] + block[counter++] * 256 + block[counter++] * 256 * 256 + block[counter++] * 256 * 256 * 256;
			}
			else
			{
				// the CRC, compressed size, and uncompressed size are stored later in the stream.
				// here, we advance the pointer.
				counter += 12;
			}

			var filenameLength = (short)(block[counter++] + block[counter++] * 256);
			var extraFieldLength = (short)(block[counter++] + block[counter++] * 256);

			block = new byte[filenameLength];
			stream.Read(block, 0, block.Length);
			entry.FileName = SharedZipFunctionality.StringFromBuffer(block, 0, block.Length);

			entry._extra = new byte[extraFieldLength];
			stream.Read(entry._extra, 0, entry._extra.Length);

			// transform the time data into something usable
			entry.LastModified = SharedZipFunctionality.PackedToDateTime(entry._lastModDateTime);

			// actually get the compressed size and CRC if necessary
			if ((entry.BitField & 0x0008) == 0x0008)
			{
				long position = stream.Position;
				long sizeOfDataRead = SharedZipFunctionality.FindSignature(stream, ZipEntryDataDescriptorSignature);
				if (sizeOfDataRead == -1) return false;

				// read 3x 4-byte fields (CRC, Compressed Size, Uncompressed Size)
				block = new byte[12];
				bytesRead = stream.Read(block, 0, block.Length);
				if (bytesRead != 12) return false;
				counter = 0;
				entry._crc32 = block[counter++] + block[counter++] * 256 + block[counter++] * 256 * 256 + block[counter++] * 256 * 256 * 256;
				entry.CompressedSize = block[counter++] + block[counter++] * 256 + block[counter++] * 256 * 256 + block[counter++] * 256 * 256 * 256;
				entry.UncompressedSize = block[counter++] + block[counter++] * 256 + block[counter++] * 256 * 256 + block[counter++] * 256 * 256 * 256;

				if (sizeOfDataRead != entry.CompressedSize) throw new Exception("Data format error (bit 3 is set)");

				// seek back to previous position, to read file data
				stream.Seek(position, SeekOrigin.Begin);
			}

			return true;
		}


		/// <summary>
		/// Returns false if the signature is not a valid ZIP entry signature
		/// </summary>
		/// <param name="signature">The signature.</param>
		/// <returns></returns>
		private static bool SignatureIsNotValid(int signature)
		{
			return (signature != ZipEntrySignature);
		}

		/// <summary>
		/// Reads the entry from the stream
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <returns></returns>
		public static ZipEntry Read(Stream stream)
		{
			var entry = new ZipEntry();
			if (!ReadHeader(stream, entry)) return null;

			entry._fileData = new byte[entry.CompressedSize];
			int bytesRead = stream.Read(entry._fileData, 0, entry._fileData.Length);
			if (bytesRead != entry._fileData.Length)
			{
				throw new Exception("Badly formatted zip file.");
			}
			// finally, seek past the (already read) Data descriptor if necessary
			if ((entry.BitField & 0x0008) == 0x0008)
			{
				stream.Seek(16, SeekOrigin.Current);
			}
			return entry;
		}

		/// <summary>
		/// Creates a ZIP entry for the specified file name
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns></returns>
		internal static ZipEntry Create(string fileName)
		{
			var entry = new ZipEntry {FileName = fileName, LastModified = File.GetLastWriteTime(fileName)};

			// adjust the time if the .NET BCL thinks it is in DST.  
			// see the note elsewhere in this file for more info. 
		    entry._lastModDateTime = SharedZipFunctionality.DateTimeToPacked(entry.LastModified.IsDaylightSavingTime() ? entry.LastModified.AddHours(-1) : entry.LastModified);

		    // we don't actually slurp in the file until the caller invokes Write on this entry.

			return entry;
		}

		/// <summary>
		/// Creates the specified bytes to compress.
		/// </summary>
		/// <param name="bytesToCompress">The bytes to compress.</param>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="fileDate">The file date.</param>
		/// <returns></returns>
		internal static ZipEntry Create(byte[] bytesToCompress, string fileName, DateTime fileDate)
		{
			var entry = new ZipEntry {FileName = fileName, BytesToCompres = bytesToCompress, LastModified = fileDate};

			// adjust the time if the .NET BCL thinks it is in DST.  
			// see the note elsewhere in this file for more info. 
		    entry._lastModDateTime = SharedZipFunctionality.DateTimeToPacked(entry.LastModified.IsDaylightSavingTime() ? entry.LastModified.AddHours(-1) : entry.LastModified);

		    // we don't actually slurp in the file until the caller invokes Write on this entry.

			return entry;
		}

		/// <summary>
		/// Extracts the current ZIP file entry
		/// </summary>
		public void Extract()
		{
			Extract(".");
		}

		/// <summary>
		/// Extracts the current ZIP file entry to the provided stream
		/// </summary>
		/// <param name="stream">The stream.</param>
		public void Extract(Stream stream)
		{
			Extract(null, stream);
		}

		/// <summary>
		/// Extracts to the specified base directory.
		/// </summary>
		/// <param name="baseDirectory">The base directory.</param>
		public void Extract(string baseDirectory)
		{
			Extract(baseDirectory, null);
		}

		/// <summary>
		/// Extracts to the specified base directory or stream.
		/// </summary>
		/// <param name="baseDirectory">The base directory.</param>
		/// <param name="stream">The stream.</param>
		/// <remarks>
		/// pass in either baseDirectory or stream, but not both. 
		/// In other words, you can extract to a stream or to a directory, but not both!
		/// </remarks>
		private void Extract(string baseDirectory, Stream stream)
		{
			string targetFile = null;
			if (baseDirectory != null)
			{
				targetFile = Path.Combine(baseDirectory, FileName);

				// check if a directory
				if (FileName.EndsWith("/"))
				{
					if (!Directory.Exists(targetFile))
					{
						Directory.CreateDirectory(targetFile);
					}
					return;
				}
			}
			else if (stream != null)
			{
				if (FileName.EndsWith("/"))
					// extract a directory to streamwriter?  nothing to do!
					return;
			}
			else throw new Exception("Invalid input.");


			using (var memstream = new MemoryStream(_fileData))
			{
				Stream input = null;
				try
				{
					if (CompressedSize == UncompressedSize)
					{
						// the DeflateStream class does not handle uncompressed data.
						// so if an entry is not compressed, then we just translate the bytes directly.
						input = memstream;
					}
					else
						input = new DeflateStream(memstream, CompressionMode.Decompress);


					if (targetFile != null)
						// ensure the target path exists
						if (!Directory.Exists(Path.GetDirectoryName(targetFile)))
							Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

					Stream output = null;
					try
					{
						if (targetFile != null)
							output = new FileStream(targetFile, FileMode.CreateNew);
						else
							output = stream;


						var bytes = new byte[4096];
						int n;

						n = 1; // anything non-zero
						while (n != 0)
						{
							n = input.Read(bytes, 0, bytes.Length);
							if (n > 0)
							{
								output.Write(bytes, 0, n);
							}
						}
					}
					finally
					{
						// we only close the output stream if we opened it. 
						if ((output != null) && (targetFile != null))
						{
							output.Close();
							output.Dispose();
						}
					}

					if (targetFile != null)
					{
						// We may have to adjust the last modified time to compensate
						// for differences in how the .NET Base Class Library deals
						// with daylight saving time (DST) versus how the Windows
						// filesystem deals with daylight saving time. See 
						// http://blogs.msdn.com/oldnewthing/archive/2003/10/24/55413.aspx for some context. 

						// in a nutshell: Daylight savings time rules change regularly.  In
						// 2007, for example, the inception week of DST changed.  In 1977,
						// DST was in place all year round. in 1945, likewise.  And so on.
						// Win32 does not attempt to guess which time zone rules were in
						// effect at the time in question.  It will render a time as
						// "standard time" and allow the app to change to DST as necessary.
						//  .NET makes a different choice.

						// -------------------------------------------------------
						// Compare the output of FileInfo.LastWriteTime.ToString("f") with
						// what you see in the property sheet for a file that was last
						// written to on the other side of the DST transition. For example,
						// suppose the file was last modified on October 17, during DST but
						// DST is not currently in effect. Explorer's file properties
						// reports Thursday, October 17, 2003, 8:45:38 AM, but .NETs
						// FileInfo reports Thursday, October 17, 2003, 9:45 AM.

						// Win32 says, "Thursday, October 17, 2002 8:45:38 AM PST". Note:
						// Pacific STANDARD Time. Even though October 17 of that year
						// occurred during Pacific Daylight Time, Win32 displays the time as
						// standard time because that's what time it is NOW.

						// .NET BCL assumes that the current DST rules were in place at the
						// time in question.  So, .NET says, "Well, if the rules in effect
						// now were also in effect on October 17, 2003, then that would be
						// daylight time" so it displays "Thursday, October 17, 2003, 9:45
						// AM PDT" - daylight time.

						// So .NET gives a value which is more intuitively correct, but is
						// also potentially incorrect, and which is not invertible. Win32
						// gives a value which is intuitively incorrect, but is strictly
						// correct.
						// -------------------------------------------------------

						// With this adjustment, I add one hour to the tweaked .NET time, if
						// necessary.  That is to say, if the time in question had occurred
						// in what the .NET BCL assumed to be DST (an assumption that may be
						// wrong given the constantly changing DST rules).

						if (LastModified.IsDaylightSavingTime())
						{
							var adjustedLastModified = LastModified + new TimeSpan(1, 0, 0);
							File.SetLastWriteTime(targetFile, adjustedLastModified);
						}
						else
							File.SetLastWriteTime(targetFile, LastModified);
					}

				}
				finally
				{
					// we only close the output stream if we opened it. 
					// we cannot use using() here because in some cases we do not want to Dispose the stream!
					if ((input != null) && (input != memstream))
					{
						input.Close();
						input.Dispose();
					}
				}
			}
		}


		/// <summary>
		/// Writes the central directory entry to the provided stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		internal void WriteCentralDirectoryEntry(Stream stream)
		{
			var bytes = new byte[4096];
			int byteCounter = 0;
			// signature
			bytes[byteCounter++] = ZipDirEntry.ZipDirectoryEntrySignature & 0x000000FF;
			bytes[byteCounter++] = (ZipDirEntry.ZipDirectoryEntrySignature & 0x0000FF00) >> 8;
			bytes[byteCounter++] = (ZipDirEntry.ZipDirectoryEntrySignature & 0x00FF0000) >> 16;
			bytes[byteCounter++] = (byte)((ZipDirEntry.ZipDirectoryEntrySignature & 0xFF000000) >> 24);

			// Version Made By
			bytes[byteCounter++] = Header[4];
			bytes[byteCounter++] = Header[5];

			// Version Needed, Bitfield, compression method, lastmod,
			// crc, sizes, filename length and extra field length -
			// are all the same as the local file header. So just copy them
			int byteCounter2;
			for (byteCounter2 = 0; byteCounter2 < 26; byteCounter2++)
				bytes[byteCounter + byteCounter2] = Header[4 + byteCounter2];

			byteCounter += byteCounter2;  // positioned at next available byte

			// File Comment Length
			bytes[byteCounter++] = 0;
			bytes[byteCounter++] = 0;

			// Disk number start
			bytes[byteCounter++] = 0;
			bytes[byteCounter++] = 0;

			// internal file attrs
			bytes[byteCounter++] = 1;
			bytes[byteCounter++] = 0;

			// external file attrs
			bytes[byteCounter++] = 0x20;
			bytes[byteCounter++] = 0;
			bytes[byteCounter++] = 0xb6;
			bytes[byteCounter++] = 0x81;

			// relative offset of local header (I think this can be zero)
			bytes[byteCounter++] = (byte)(_relativeOffsetOfHeader & 0x000000FF);
			bytes[byteCounter++] = (byte)((_relativeOffsetOfHeader & 0x0000FF00) >> 8);
			bytes[byteCounter++] = (byte)((_relativeOffsetOfHeader & 0x00FF0000) >> 16);
			bytes[byteCounter++] = (byte)((_relativeOffsetOfHeader & 0xFF000000) >> 24);

			// actual filename (starts at offset 34 in header) 
		    for (byteCounter2 = 0; byteCounter2 < Header.Length - 30; byteCounter2++)
		        bytes[byteCounter + byteCounter2] = Header[30 + byteCounter2];
		    byteCounter += byteCounter2;

			stream.Write(bytes, 0, byteCounter);
		}


		/// <summary>
		/// Writes the header to the provided stream
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="bytes">The bytes.</param>
		private void WriteHeader(Stream stream, byte[] bytes)
		{
			// write the header info

			int byteCounter = 0;
			// signature
			bytes[byteCounter++] = ZipEntrySignature & 0x000000FF;
			bytes[byteCounter++] = (ZipEntrySignature & 0x0000FF00) >> 8;
			bytes[byteCounter++] = (ZipEntrySignature & 0x00FF0000) >> 16;
			bytes[byteCounter++] = (byte)((ZipEntrySignature & 0xFF000000) >> 24);

			// version needed
			const short fixedVersionNeeded = 0x14; // from examining existing zip files
			bytes[byteCounter++] = fixedVersionNeeded & 0x00FF;
			bytes[byteCounter++] = (fixedVersionNeeded & 0xFF00) >> 8;

			// bitfield
			const short bitField = 0x00; // from examining existing zip files
			bytes[byteCounter++] = bitField & 0x00FF;
			bytes[byteCounter++] = (bitField & 0xFF00) >> 8;

			// compression method
			const short compressionMethod = 0x08; // 0x08 = Deflate
			bytes[byteCounter++] = compressionMethod & 0x00FF;
			bytes[byteCounter++] = (compressionMethod & 0xFF00) >> 8;

			// LastMod
			bytes[byteCounter++] = (byte)(_lastModDateTime & 0x000000FF);
			bytes[byteCounter++] = (byte)((_lastModDateTime & 0x0000FF00) >> 8);
			bytes[byteCounter++] = (byte)((_lastModDateTime & 0x00FF0000) >> 16);
			bytes[byteCounter++] = (byte)((_lastModDateTime & 0xFF000000) >> 24);

			// CRC32 (Int32)
			var crc32 = new CRC32();
			Stream input;
			if (BytesToCompres == null || BytesToCompres.Length == 0)
				input = File.OpenRead(FileName);
			else
				input = new MemoryStream(BytesToCompres);
			uint crc = crc32.GetCrc32AndCopy(input, CompressedStream);
			input.Dispose();
			CompressedStream.Close();  // to get the footer bytes written to the underlying stream

			bytes[byteCounter++] = (byte)(crc & 0x000000FF);
			bytes[byteCounter++] = (byte)((crc & 0x0000FF00) >> 8);
			bytes[byteCounter++] = (byte)((crc & 0x00FF0000) >> 16);
			bytes[byteCounter++] = (byte)((crc & 0xFF000000) >> 24);

			// CompressedSize (Int32)
			var isz = (Int32)_underlyingMemoryStream.Length;
			var sz = (UInt32)isz;
			bytes[byteCounter++] = (byte)(sz & 0x000000FF);
			bytes[byteCounter++] = (byte)((sz & 0x0000FF00) >> 8);
			bytes[byteCounter++] = (byte)((sz & 0x00FF0000) >> 16);
			bytes[byteCounter++] = (byte)((sz & 0xFF000000) >> 24);

			// UncompressedSize (Int32)
			bytes[byteCounter++] = (byte)(crc32.TotalBytesRead & 0x000000FF);
			bytes[byteCounter++] = (byte)((crc32.TotalBytesRead & 0x0000FF00) >> 8);
			bytes[byteCounter++] = (byte)((crc32.TotalBytesRead & 0x00FF0000) >> 16);
			bytes[byteCounter++] = (byte)((crc32.TotalBytesRead & 0xFF000000) >> 24);

			// filename length (Int16)
			var compressedFileName = CompressedFileName;
			var length = (Int16)compressedFileName.Length;
			// see note below about TrimVolumeFromFullyQualifiedPaths.
			if ((compressedFileName[1] == ':') && (compressedFileName[2] == '\\'))
				length -= 3;
			bytes[byteCounter++] = (byte)(length & 0x00FF);
			bytes[byteCounter++] = (byte)((length & 0xFF00) >> 8);

			// extra field length (short)
			const int extraFieldLength = 0x00;
			bytes[byteCounter++] = extraFieldLength & 0x00FF;
			bytes[byteCounter++] = (extraFieldLength & 0xFF00) >> 8;

			// Tue, 27 Mar 2007  16:35

			// Creating a zip that contains entries with "fully qualified" pathnames
			// can result in a zip archive that is unreadable by Windows Explorer.
			// Such archives are valid according to other tools but not to explorer.
			// To avoid this, we can trim off the leading volume name and slash (eg
			// c:\) when creating (writing) a zip file.  We do this by default and we
			// leave the old behavior available with the
			// TrimVolumeFromFullyQualifiedPaths flag - set it to false to get the old
			// behavior.  It only affects zip creation.

			// actual filename
			// trim off volume letter, colon, and slash
			string fileName = CompressedFileName;
			char[] c = ((fileName[1] == ':') && (fileName[2] == '\\')) ? fileName.Substring(3).ToCharArray() : fileName.ToCharArray();
			int fileCharCounter;

			for (fileCharCounter = 0; (fileCharCounter < c.Length) && (byteCounter + fileCharCounter < bytes.Length); fileCharCounter++)
				bytes[byteCounter + fileCharCounter] = BitConverter.GetBytes(c[fileCharCounter])[0];

			byteCounter += fileCharCounter;

			// extra field (we always write nothing in this implementation)

			// remember the file offset of this header
			_relativeOffsetOfHeader = (int)stream.Length;

			// finally, write the header to the stream
			stream.Write(bytes, 0, byteCounter);

			// preserve this header data for use with the central directory structure.
			Header = new byte[byteCounter];
		    for (fileCharCounter = 0; fileCharCounter < byteCounter; fileCharCounter++)
		        Header[fileCharCounter] = bytes[fileCharCounter];
		}


		/// <summary>
		/// Writes the specified out stream.
		/// </summary>
		/// <param name="outStream">The out stream.</param>
		internal void Write(Stream outStream)
		{
			var bytes = new byte[4096];

			// write the header:
			WriteHeader(outStream, bytes);

			// write the actual file data: 
			_underlyingMemoryStream.Position = 0;

			while (true)
			{
				int bytesRead = _underlyingMemoryStream.Read(bytes, 0, bytes.Length);
				if (bytesRead == 0) break;
				outStream.Write(bytes, 0, bytesRead);
			}

			_underlyingMemoryStream.Close();
			_underlyingMemoryStream.Dispose();
			_underlyingMemoryStream = null;
		}
	}

	/// <summary>
	/// ZIP directory entry
	/// </summary>
	public class ZipDirEntry
	{
		/// <summary>
		/// ZIP directory entry signature
		/// </summary>
		internal const int ZipDirectoryEntrySignature = 0x02014b50;

		/// <summary>
		/// Initializes a new instance of the <see cref="ZipDirEntry"/> class.
		/// </summary>
		private ZipDirEntry() { }
		/// <summary>
		/// Gets or sets the last modified date.
		/// </summary>
		/// <value>The last modified.</value>
		public DateTime LastModified { get; private set; }
		/// <summary>
		/// Gets or sets the name of the file.
		/// </summary>
		/// <value>The name of the file.</value>
		public string FileName { get; private set; }
		/// <summary>
		/// Gets or sets the comment.
		/// </summary>
		/// <value>The comment.</value>
		public string Comment { get; private set; }
		/// <summary>
		/// Gets or sets the version made by.
		/// </summary>
		/// <value>The version made by.</value>
		public Int16 VersionMadeBy { get; private set; }
		/// <summary>
		/// Gets or sets the version needed.
		/// </summary>
		/// <value>The version needed.</value>
		public Int16 VersionNeeded { get; private set; }
		/// <summary>
		/// Gets or sets the compression method.
		/// </summary>
		/// <value>The compression method.</value>
		public Int16 CompressionMethod { get; private set; }
		/// <summary>
		/// Gets or sets the size of the compressed.
		/// </summary>
		/// <value>The size of the compressed.</value>
		public Int32 CompressedSize { get; private set; }
		/// <summary>
		/// Gets or sets the size of the uncompressed.
		/// </summary>
		/// <value>The size of the uncompressed.</value>
		public Int32 UncompressedSize { get; private set; }

	    /// <summary>
	    /// Gets the compression ratio.
	    /// </summary>
	    /// <value>The compression ratio.</value>
	    public Double CompressionRatio
	    {
	        get { return 100*(1.0 - (1.0*CompressedSize)/(1.0*UncompressedSize)); }
	    }

	    private Int16 _bitField;
		private Int32 _lastModDateTime;
		private Int32 _crc32;
		private byte[] _extra;

		/// <summary>
		/// Initializes a new instance of the <see cref="ZipDirEntry"/> class.
		/// </summary>
		/// <param name="zipEntry">The zip entry.</param>
		internal ZipDirEntry(ZipEntry zipEntry) { }

		/// <summary>
		/// Reads the specified entry stream.
		/// </summary>
		/// <param name="entryStream">The entry stream.</param>
		/// <returns></returns>
		public static ZipDirEntry Read(Stream entryStream)
		{

			int signature = SharedZipFunctionality.ReadSignature(entryStream);
			// return null if this is not a local file header signature
			if (SignatureIsNotValid(signature))
			{
				entryStream.Seek(-4, SeekOrigin.Current);
				return null;
			}

			var block = new byte[42];
			var bytesRead = entryStream.Read(block, 0, block.Length);
			if (bytesRead != block.Length) return null;

			var counter = 0;
			var zipDirectoryEntry = new ZipDirEntry();

			zipDirectoryEntry.VersionMadeBy = (short)(block[counter++] + block[counter++] * 256);
			zipDirectoryEntry.VersionNeeded = (short)(block[counter++] + block[counter++] * 256);
			zipDirectoryEntry._bitField = (short)(block[counter++] + block[counter++] * 256);
			zipDirectoryEntry.CompressionMethod = (short)(block[counter++] + block[counter++] * 256);
			zipDirectoryEntry._lastModDateTime = block[counter++] + block[counter++] * 256 + block[counter++] * 256 * 256 + block[counter++] * 256 * 256 * 256;
			zipDirectoryEntry._crc32 = block[counter++] + block[counter++] * 256 + block[counter++] * 256 * 256 + block[counter++] * 256 * 256 * 256;
			zipDirectoryEntry.CompressedSize = block[counter++] + block[counter++] * 256 + block[counter++] * 256 * 256 + block[counter++] * 256 * 256 * 256;
			zipDirectoryEntry.UncompressedSize = block[counter++] + block[counter++] * 256 + block[counter++] * 256 * 256 + block[counter++] * 256 * 256 * 256;

			zipDirectoryEntry.LastModified = SharedZipFunctionality.PackedToDateTime(zipDirectoryEntry._lastModDateTime);

			var filenameLength = (short)(block[counter++] + block[counter++] * 256);
			var extraFieldLength = (short)(block[counter++] + block[counter++] * 256);
			var commentLength = (short)(block[counter++] + block[counter++] * 256);
			var diskNumber = (short)(block[counter++] + block[counter++] * 256);
			var internalFileAttrs = (short)(block[counter++] + block[counter++] * 256);
			var externalFileAttrs = block[counter++] + block[counter++] * 256 + block[counter++] * 256 * 256 + block[counter++] * 256 * 256 * 256;
			var offset = block[counter++] + block[counter++] * 256 + block[counter++] * 256 * 256 + block[counter++] * 256 * 256 * 256;

			block = new byte[filenameLength];
			entryStream.Read(block, 0, block.Length);
			zipDirectoryEntry.FileName = SharedZipFunctionality.StringFromBuffer(block, 0, block.Length);

			zipDirectoryEntry._extra = new byte[extraFieldLength];
			entryStream.Read(zipDirectoryEntry._extra, 0, zipDirectoryEntry._extra.Length);

			block = new byte[commentLength];
			entryStream.Read(block, 0, block.Length);
			zipDirectoryEntry.Comment = SharedZipFunctionality.StringFromBuffer(block, 0, block.Length);

			return zipDirectoryEntry;
		}

		/// <summary>
		/// Returns false if the directory signature is not a valid ZIP directory signature
		/// </summary>
		/// <param name="signature">The signature.</param>
		/// <returns></returns>
		private static bool SignatureIsNotValid(int signature)
		{
			return (signature != ZipDirectoryEntrySignature);
		}
	}

	/// <summary>
	/// Shared functionality
	/// </summary>
	public class SharedZipFunctionality
	{
		/// <summary>
		/// Returns a string of specified location and length from the buffer.
		/// </summary>
		/// <param name="buf">The buf.</param>
		/// <param name="start">The start.</param>
		/// <param name="maxLength">Length of the max.</param>
		/// <returns></returns>
		protected internal static string StringFromBuffer(byte[] buf, int start, int maxLength)
		{
			int bufferCounter;
			var character = new char[maxLength];
		    for (bufferCounter = 0; (bufferCounter < maxLength) && (bufferCounter < buf.Length) && (buf[bufferCounter] != 0); bufferCounter++)
		        character[bufferCounter] = (char)buf[bufferCounter];
		    return new String(character, 0, bufferCounter);
		}

		/// <summary>
		/// Reads the signature from the provided stream.
		/// </summary>
		/// <param name="signatureStream">The signature stream.</param>
		/// <returns></returns>
		protected internal static int ReadSignature(Stream signatureStream)
		{
		    var signatureBytes = new byte[4];
			var counter = signatureStream.Read(signatureBytes, 0, signatureBytes.Length);
			if (counter != signatureBytes.Length) throw new Exception("Could not read signature - no data!");
			var signature = (((signatureBytes[3] * 256 + signatureBytes[2]) * 256) + signatureBytes[1]) * 256 + signatureBytes[0];
			return signature;
		}

		/// <summary>
		/// Finds the signature in the provided stream.
		/// </summary>
		/// <param name="signatureStream">The signature stream.</param>
		/// <param name="signatureToFind">The signature to find.</param>
		/// <returns></returns>
		protected internal static long FindSignature(Stream signatureStream, int signatureToFind)
		{
			long startingPosition = signatureStream.Position;

			const int batchSize = 1024;
			var targetBytes = new byte[4];
			targetBytes[0] = (byte)(signatureToFind >> 24);
			targetBytes[1] = (byte)((signatureToFind & 0x00FF0000) >> 16);
			targetBytes[2] = (byte)((signatureToFind & 0x0000FF00) >> 8);
			targetBytes[3] = (byte)(signatureToFind & 0x000000FF);
			var batch = new byte[batchSize];
		    bool success = false;
			do
			{
				int counter = signatureStream.Read(batch, 0, batch.Length);
				if (counter != 0)
				{
				    for (int counter2 = 0; counter2 < counter; counter2++)
				        if (batch[counter2] == targetBytes[3])
				        {
				            signatureStream.Seek(counter2 - counter, SeekOrigin.Current);
				            var sig = ReadSignature(signatureStream);
				            success = (sig == signatureToFind);
				            if (!success) signatureStream.Seek(-3, SeekOrigin.Current);
				            break; // out of for loop
				        }
				}
				else break;
				if (success) break;
			} while (true);
			if (!success)
			{
				signatureStream.Seek(startingPosition, SeekOrigin.Begin);
				return -1;  // or throw?
			}

			// subtract 4 for the signature.
			long bytesRead = (signatureStream.Position - startingPosition) - 4;
			// number of bytes read, should be the same as compressed size of file            
			return bytesRead;
		}

		/// <summary>
		/// Turns a packed date time into a .NET date time
		/// </summary>
		/// <param name="packedDateTime">The packed date time.</param>
		/// <returns></returns>
		protected internal static DateTime PackedToDateTime(Int32 packedDateTime)
		{
			var packedTime = (Int16)(packedDateTime & 0x0000ffff);
			var packedDate = (Int16)((packedDateTime & 0xffff0000) >> 16);

			var year = 1980 + ((packedDate & 0xFE00) >> 9);
			var month = (packedDate & 0x01E0) >> 5;
			var day = packedDate & 0x001F;

			var hour = (packedTime & 0xF800) >> 11;
			var minute = (packedTime & 0x07E0) >> 5;
			var second = packedTime & 0x001F;

			var date = DateTime.Now;
			try { date = new DateTime(year, month, day, hour, minute, second, 0); }
			catch
			{
				Console.Write("\nInvalid date/time?:\nyear: {0} ", year);
				Console.Write("month: {0} ", month);
				Console.WriteLine("day: {0} ", day);
				Console.WriteLine("HH:MM:SS= {0}:{1}:{2}", hour, minute, second);
			}

			return date;
		}

		/// <summary>
		/// Turns a .NET date time into a packed date time
		/// </summary>
		/// <param name="time">The time.</param>
		/// <returns></returns>
		protected internal static Int32 DateTimeToPacked(DateTime time)
		{
			UInt16 packedDate = (UInt16)((time.Day & 0x0000001F) | ((time.Month << 5) & 0x000001E0) | (((time.Year - 1980) << 9) & 0x0000FE00));
			UInt16 packedTime = (UInt16)((time.Second & 0x0000001F) | ((time.Minute << 5) & 0x000007E0) | ((time.Hour << 11) & 0x0000F800));
			return (Int32)(((UInt32)(packedDate << 16)) | packedTime);
		}
	}

	/// <summary>
	/// Calculates a 32bit Cyclic Redundancy Checksum (CRC) using the
	/// same polynomial used by Zip.
	/// </summary>
	public class CRC32
	{
		private readonly UInt32[] _crc32Table;
		private const int BufferSize = 8192;

		private Int32 _totalBytesRead;

	    /// <summary>
	    /// Gets the total bytes read.
	    /// </summary>
	    /// <value>The total bytes read.</value>
	    public Int32 TotalBytesRead
	    {
	        get { return _totalBytesRead; }
	    }

	    /// <summary>
		/// Returns the CRC32 for the specified stream.
		/// </summary>
		/// <param name="input">The stream over which to calculate the CRC32</param>
		/// <returns>the CRC32 calculation</returns>
		public UInt32 GetCrc32(Stream input)
		{
			return GetCrc32AndCopy(input, null);
		}

		/// <summary>
		/// Returns the CRC32 for the specified stream, and writes the input into the output stream.
		/// </summary>
		/// <param name="input">The stream over which to calculate the CRC32</param>
		/// <param name="output">The stream into which to deflate the input</param>
		/// <returns>the CRC32 calculation</returns>
		public UInt32 GetCrc32AndCopy(Stream input, Stream output)
		{
			unchecked
			{
			    uint crc32Result = 0xFFFFFFFF;
				var buffer = new byte[BufferSize];
				const int readSize = BufferSize;

				_totalBytesRead = 0;
				int count = input.Read(buffer, 0, readSize);
				if (output != null) output.Write(buffer, 0, count);
				_totalBytesRead += count;
				while (count > 0)
				{
				    for (int i = 0; i < count; i++)
				        crc32Result = ((crc32Result) >> 8) ^ _crc32Table[(buffer[i]) ^ ((crc32Result) & 0x000000FF)];
				    count = input.Read(buffer, 0, readSize);
					if (output != null) output.Write(buffer, 0, count);
					_totalBytesRead += count;
				}

				return ~crc32Result;
			}
		}


		/// <summary>
		/// Construct an instance of the CRC32 class, pre-initialising the table
		/// for speed of lookup.
		/// </summary>
		public CRC32()
		{
			unchecked
			{
				// This is the official polynomial used by CRC32 in PKZip.
				// Often the polynomial is shown reversed as 0x04C11DB7.
				const uint dwPolynomial = 0xEDB88320;
				UInt32 i;

			    _crc32Table = new UInt32[256];

				UInt32 dwCrc;
				for (i = 0; i < 256; i++)
				{
					dwCrc = i;
				    UInt32 j;
				    for (j = 8; j > 0; j--)
					{
					    if ((dwCrc & 1) == 1)
					        dwCrc = (dwCrc >> 1) ^ dwPolynomial;
					    else
					        dwCrc >>= 1;
					}
					_crc32Table[i] = dwCrc;
				}
			}
		}
	}
}
