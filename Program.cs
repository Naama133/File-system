using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;

namespace BGUFS
{
	class Program
	{
		
		static void Main(string[] args)
		{
			if (args.Length == 0)
            {
                Console.WriteLine("No Arguments!");
                System.Environment.Exit(1);
            }

            var command = args[0];

			switch (command)
            {
                case "-create" when args.Length == 2: //1
                    create(args[1]);
                    break;

                case "-add" when args.Length == 3: //2
                    add(args[1], args[2]);
                    break;

                case "-remove" when args.Length == 3: //3
                    remove(args[1], args[2]);
                    break;

                case "-rename" when args.Length == 4: //4
                    rename(args[1], args[2], args[3]);
                    break;

                case "-extract" when args.Length == 4: //5
                    extract(args[1], args[2], args[3]);
                    break;

                case "-dir" when args.Length == 2: //6
                    dir(args[1]);
                    break;

                case "-hash" when args.Length == 3: //7
                    hash(args[1], args[2]);
                    break;

                case "-optimize" when args.Length == 2: //8
					optimize(args[1]);
					break;

                case "-sortAB" when args.Length == 2: //9
                    sortAB(args[1]);
                    break;

                case "-sortDate" when args.Length == 2: //10
                    sortDate(args[1]);
                    break;

                case "-sortSize" when args.Length == 2: //11
                    sortSize(args[1]);
                    break;

                case "-addLink" when args.Length == 4: //12
                    addLink(args[1], args[2], args[3]);
                    break;

                default:
                    Console.WriteLine("Invalid command");
                    break;
            }

        }

		//create new Fily System, with the given name (argument to the function) -> we can assume that the filename does not exist already
		static void create(string filesystem)
		{
			// Check if file already exists. If yes, do not create another one - > we don't really need to check this case     
			if (!File.Exists(filesystem))
			{
				string Text = "BGUFS_0:,EndOfHeader:61,StartOfFiles:20001,EndOfFiles:20001$\n"; //create the file of the file-system, and write to it
				File.WriteAllText(filesystem, Text);
			}
			else
			{
				Console.WriteLine("File System is already exist!"); //we don't really need to check this case     
			}

		}

		//Add a file <filename> to the filesystem, If the filename already exists in the filesystem, print an error message: "file already exist", and exit
		//The filesystem supports all types of files (pdf, word, etc..)
		static void add(string filesystem, string filename)
		{

			if (!File.Exists(filesystem)) //check file system exist
			{
				Console.WriteLine("File System does not exist");
				System.Environment.Exit(1);
			}

			// Check if file exists.     
			if (!File.Exists(filename))
			{
				Console.WriteLine("file does not exist");
				System.Environment.Exit(1);
			}

			
			Boolean isFirst = false; //sign the first file that copied into the file system

			//make sure this file system have the right forams - BGUFS_
			long FileSystemSize = 0;
			StringBuilder contentOfSystem = new StringBuilder(); ;
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				FileSystemSize = sr.BaseStream.Length; //find the length of the text
				if (FileSystemSize < 6)
				{ //if file is empty (first insertion)
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
				var buffer = new Char[Math.Min(FileSystemSize, contentOfSystem.MaxCapacity)];  //create new buffer of 6 chars
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				contentOfSystem.Append(buffer);
				string checkBegin = "";
				for (int i = 0; i < 6; i++)
				{
					checkBegin += buffer[i];
				}
				if (!checkBegin.Equals("BGUFS_"))
				{
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
			}

			if (contentOfSystem[6] == '0') //check if it's the first time of adding file into the system 
			{
				isFirst = true;
			}

			string sbStr = contentOfSystem.ToString(); //string of content (head of file system)
			int filesCounter = 0; //how many files we have in the system
			ArrayList header = new ArrayList();
			//build the header lines list
			if (!isFirst)
			{
				header = getHeader(filesystem, contentOfSystem); //create header object
				filesCounter = header.Count;
				for (int i = 0; i < header.Count; i++)
				{
					string name = new System.IO.FileInfo(filename).Name;
					if (name.Equals(((FileInfo)header[i]).getFileName()))
					{
						Console.WriteLine("file already exist"); //if this file already exit -> print error and return
						System.Environment.Exit(1);
					}
				}
			}

			int indexEnd = SaerchInText(contentOfSystem, "EndOfHeader");
			int indexStart = SaerchInText(contentOfSystem, "StartOfFiles");
			int indexEndFiles = SaerchInText(contentOfSystem, "EndOfFiles");
			int indexSign = SaerchInText(contentOfSystem, "$");
			int size = (indexStart - indexEnd - 12 - 1);
			string val = sbStr.Substring(indexEnd + 12, size);//end of header section
			long endHeader = Convert.ToInt64(val);//end of header section
			size = (indexEndFiles - indexStart - 13 - 1);
			val = sbStr.Substring(indexStart + 13, size);//end of header section
			long baseFilesAdress = Convert.ToInt64(val);//end of header section
			size = (indexSign - indexEndFiles - 11);
			val = sbStr.Substring(indexEndFiles + 11, size);//end of header section
			long endFilesAdress = Convert.ToInt64(val);//end of header section

			//read the new file content into string builder
			Byte[] bytes = File.ReadAllBytes(filename); //We do not have to open the file for reading
			String newFileContent = Convert.ToBase64String(bytes);
			int len = newFileContent.Length;
			long baseCurrFileAdress;
			if (isFirst)
			{
				baseCurrFileAdress = baseFilesAdress;
			}
			else
			{
				baseCurrFileAdress = insertInGap(filesystem, len, contentOfSystem); //find the insertion point for this file content
				if (baseCurrFileAdress < 0)
				{ //that means we have no free space for this file in gaps
					baseCurrFileAdress = endFilesAdress + 1;
				}
			}
			long endCurrFileAddress = baseCurrFileAdress + newFileContent.Length - 1; // end of current file address

			//add the new file to the header
			FileInfo node = new FileInfo(filename);
			node.setFileSize(newFileContent.Length.ToString());
			node.setFileAddress(baseCurrFileAdress);
			header.Add(node);

			//save length of old files of the first line
			int LastFilesNumberLen = filesCounter.ToString().Length;
			int LastEndHeaderLen = endHeader.ToString().Length;
			int LastBaseFilesLen = baseFilesAdress.ToString().Length;
			int LastEndFilesLen = endFilesAdress.ToString().Length;

			//append into the filesystem the new file (filename) content
			WriteTextToIndex(filesystem, baseCurrFileAdress, newFileContent);


			endFilesAdress = Math.Max(endFilesAdress, endCurrFileAddress); //update the end files address

			//update header
			string newFileMetaDate = node.infoFileToString("regular");
			newFileMetaDate += "\n";
			if (endHeader > (baseFilesAdress - newFileMetaDate.Length - endFilesAdress.ToString().Length)) //make sure we have space for updates to
			{
				FileInfo firstFile = firstAddressNode(filesystem, header); //returns the node in header that that starts first
				addTextToEnd(firstFile, endFilesAdress, baseFilesAdress, endHeader, filesystem, contentOfSystem);
			}

			//now we have space for the new string in the header section
			WriteTextToIndex(filesystem, endHeader, newFileMetaDate);

			endHeader = endHeader + newFileMetaDate.Length; //update the end of the header
			filesCounter++; //now we have an extra file in the system

			//update first line
			//check if the new number is longer then last number in all of those fields
			int NewFilesNumberLen = filesCounter.ToString().Length;
			int NewEndHeaderLen = endHeader.ToString().Length;
			int NewBaseFilesLen = baseFilesAdress.ToString().Length;
			int NewEndFilesLen = endFilesAdress.ToString().Length;

			int difFilesNumberLen = NewFilesNumberLen - LastFilesNumberLen;
			int difEndHeaderLen = NewEndHeaderLen - LastEndHeaderLen;
			int difBaseFilesLen = NewBaseFilesLen - LastBaseFilesLen;
			int difEndFilesLen = NewEndFilesLen - LastEndFilesLen;

			//check how many chars we need to add for this update
			int howMuchMore = (difFilesNumberLen + difEndHeaderLen + difBaseFilesLen + difEndFilesLen);
			if ((howMuchMore > 0) && ((endHeader + howMuchMore) > baseFilesAdress))
			{
				FileInfo firstFile = firstAddressNode(filesystem, header); //returns the node in header that that starts first
				addTextToEnd(firstFile, endFilesAdress, baseFilesAdress, endHeader, filesystem, contentOfSystem);
			}
			//need to move the header for this changes
			MoveTextForward(filesystem, 0, endHeader, howMuchMore);
			//Now we have space for this string in the header section in generally
			endHeader += howMuchMore;
			//after this function - we have the space for this row
			string newHeadLine = "BGUFS_" + filesCounter + ":,EndOfHeader:" + endHeader + ",StartOfFiles:" + baseFilesAdress + ",EndOfFiles:" + endFilesAdress + "$";
			WriteTextToIndex(filesystem, 0, newHeadLine);

		}

		//remove file from fileSystem
		static void remove(string filesystem, string filename)
		{
			if (!File.Exists(filesystem)) //check file system exist
			{
				Console.WriteLine("File System does not exist!");
				System.Environment.Exit(1);
			}
			//make sure this file system have the right forams - BGUFS_
			StringBuilder content = new StringBuilder();
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				long FileSystemSize = sr.BaseStream.Length; //find the length of the text
				var buffer = new Char[Math.Min(FileSystemSize, content.MaxCapacity)];  //create new buffer of 6 chars
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				string checkBegin = "";
				for (int i = 0; i < 6; i++)
				{
					checkBegin += buffer[i];
				}
				if (!checkBegin.Equals("BGUFS_"))
				{
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
				content.Append(buffer);
			}

			int n1 = isExist(filesystem, filename); 
			// Check if file not exists in the system. If so, print error     
			if (n1 < 0)
			{
				Console.WriteLine("file does not exist");
				System.Environment.Exit(1);
			} 

			//find this file meta data (look for it's row in the header), after check it's in the system
			ArrayList header = getHeader(filesystem, content);
			FileInfo file = null;
			for (int i = 0; i < header.Count; i++)
			{
				FileInfo tempFile = (FileInfo)header[i];
				if (tempFile.getExtention().Equals("link"))
				{
					if (tempFile.getLinkName().Equals(filename))
					{
						file = tempFile;
						break;
					}
				}
				else
				{
					if (filename.Equals(tempFile.getFileName()))
					{
						file = tempFile;
						break;
					}
				}

			}
			if (file == null)
			{
				Console.WriteLine("file does not exist");
				System.Environment.Exit(1);
			}

			Boolean isLinke = file.isAlink();
			int linksCounter = 0;
			//delete all the links to this file - relevant only for non-link removes - recursive
			if (!isLinke)
			{
				for (int i = 0; i < header.Count; i++)
				{
					FileInfo tempFile = (FileInfo)header[i];
					if (tempFile.getExtention().Equals("link"))
					{
						if (tempFile.getFileName().Equals(filename))
						{
							linksCounter++;
							remove(filesystem, tempFile.getLinkName());
						}
					}
				}
			}

			//read the updated file after delitions
			StringBuilder systemContent = new StringBuilder();
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				long FileSystemSize = sr.BaseStream.Length; //find the length of the text
				var buffer = new Char[Math.Min(FileSystemSize, systemContent.MaxCapacity)];  //create new buffer of 6 chars
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				systemContent.Append(buffer);
			}

			//system file meta data
			string sbStr = systemContent.ToString(); //string of content (head of file system)
			int indexEnd = SaerchInText(systemContent, "EndOfHeader");
			int indexStart = SaerchInText(systemContent, "StartOfFiles");
			int indexEndFiles = SaerchInText(systemContent, "EndOfFiles");
			int indexSign = SaerchInText(systemContent, "$");
			int size = (indexStart - indexEnd - 12 - 1);
			string val = sbStr.Substring(indexEnd + 12, size);//end of header section
			long endHeader = Convert.ToInt64(val);//end of header section
			long endHaderNotChange = endHeader;
			size = (indexEndFiles - indexStart - 13 - 1);
			val = sbStr.Substring(indexStart + 13, size);//end of header section
			long baseFilesAdress = Convert.ToInt64(val);//end of header section
			size = (indexSign - indexEndFiles - 11);
			val = sbStr.Substring(indexEndFiles + 11, size);//end of header section
			long endFilesAdress = Convert.ToInt64(val);//end of header section

			int indexInHeader;
			if (file.getExtention().Equals("link"))
			{
				indexInHeader = SaerchInText(systemContent, file.getLinkName()); //the index of this file row in header
			}
			else
			{
				indexInHeader = SaerchInText(systemContent, file.getFileName()); //the index of this file row in header
			}
			string temp = sbStr.Substring(indexInHeader);
			int index = temp.IndexOf('$');
			int EndindexInHeader = indexInHeader + index + 2; //the index of the end of this file row in header

			//save length of old files of the first line
			int LastFilesNumberLen = header.Count.ToString().Length;
			int LastEndHeaderLen = endHeader.ToString().Length;
			int LastEndFilesLen = endFilesAdress.ToString().Length;

			//check if we ask for delete a link
			long newEndFiles = endFilesAdress;
			if (!isLinke)
			{ //if it's not a link -> delete the content of this file
			  //extract file's start & end indexes
				long startIndex = file.getFileAddress();
				long endIndex = Convert.ToInt64(file.getFileSize()) + startIndex - 1;
				using (StreamWriter sw = new StreamWriter(File.Open(filesystem, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
				{ //delete file's content
					sw.BaseStream.Seek(startIndex, SeekOrigin.Begin);
					for (int i = 0; i < (endIndex - startIndex + 1); i++)
					{
						sw.Write(" ");
					}
					sw.Close();
				}
				//calculate the new endFilesAdress (if we delete the last file of the system)
				FileInfo lastFile = LastAddressNode(filesystem, header);
				FileInfo firstFile = firstAddressNode(filesystem, header);
				if (file.getFileAddress() == lastFile.getFileAddress())
				{ //if we have deleted the last file -> change - 1 the end files field
					if (file.getFileAddress() == firstFile.getFileAddress())
					{
						newEndFiles = file.getFileAddress();
					}
					else
					{
						FileInfo alomostlastFile = almostLastAddressNode(filesystem, header);
						newEndFiles = alomostlastFile.getFileAddress() + Convert.ToInt64(alomostlastFile.getFileSize()) - 1;
					}
				}
			}

			endFilesAdress = Math.Min(newEndFiles, endFilesAdress); //update the end files address

			//update header - delete this file row
			using (StreamWriter sw = new StreamWriter(File.Open(filesystem, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
			{ //delete file's row from header
				sw.BaseStream.Seek(indexInHeader, SeekOrigin.Begin);
				for (int i = 0; i < (EndindexInHeader - indexInHeader); i++)
				{
					sw.Write(" ");
				}
				sw.Close();
			}

			endHeader = endHeader - (EndindexInHeader - indexInHeader); //update the end of the header
			int filesCounter = header.Count - 1 - linksCounter; //now we have one less file in the system

			//update first line
			//check if the new number is shorter then last number in all of those fields
			int NewFilesNumberLen = filesCounter.ToString().Length;
			int NewEndHeaderLen = endHeader.ToString().Length;
			int NewEndFilesLen = endFilesAdress.ToString().Length;

			int difFilesNumberLen = LastFilesNumberLen - NewFilesNumberLen;
			int difEndHeaderLen = LastEndHeaderLen - NewEndHeaderLen;
			int difEndFilesLen = LastEndFilesLen - NewEndFilesLen;

			//check how many chars we need to add for this update
			int howMuchLess = (difFilesNumberLen + difEndHeaderLen + difEndFilesLen);
			endHeader -= howMuchLess;

			//create new line for system meta-data
			string newHeadLine = "BGUFS_" + filesCounter + ":,EndOfHeader:" + endHeader + ",StartOfFiles:" + baseFilesAdress + ",EndOfFiles:" + endFilesAdress + "$\n";
			WriteTextToIndex(filesystem, 0, newHeadLine); //write  this new line to the head of the system header
														  //remove the part of the header that comes after this file line backwards
			MoveTextBackwards(filesystem, EndindexInHeader, endHaderNotChange, EndindexInHeader - indexInHeader);
			//write " " instead
			using (StreamWriter sw = new StreamWriter(File.Open(filesystem, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
			{
				sw.BaseStream.Seek(endHaderNotChange - (EndindexInHeader - indexInHeader) + 1, SeekOrigin.Begin);
				for (int i = 0; i < (EndindexInHeader - indexInHeader); i++)
				{
					sw.Write(" ");
				}
				sw.Close();
			}
			//remove all header backwords
			MoveTextBackwards(filesystem, newHeadLine.Length, endHeader, howMuchLess);
			//write " " instead
			using (StreamWriter sw = new StreamWriter(File.Open(filesystem, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
			{
				sw.BaseStream.Seek(endHeader - howMuchLess + 1, SeekOrigin.Begin);
				for (int i = 0; i < (howMuchLess); i++)
				{
					sw.Write(" ");
				}
				sw.Close();
			}

		}

		static void rename(string filesystem, string filename, string newfilename)
		{
			if (!File.Exists(filesystem)) //check file system exist
			{
				Console.WriteLine("File System does not exist!");
				System.Environment.Exit(1);
			}

			//make sure this file system have the right forams - BGUFS_
			long FileSystemSize = 0;
			StringBuilder contentOfSystem = new StringBuilder(); ;
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				FileSystemSize = sr.BaseStream.Length; //find the length of the text
				if (FileSystemSize < 6)
				{ //if file is empty (first insertion)
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
				var buffer = new Char[Math.Min(FileSystemSize, contentOfSystem.MaxCapacity)];  //create new buffer of 6 chars
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				contentOfSystem.Append(buffer);
				string checkBegin = "";
				for (int i = 0; i < 6; i++)
				{
					checkBegin += buffer[i];
				}
				if (!checkBegin.Equals("BGUFS_"))
				{
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
			}
			int n1 = isExist(filesystem, filename);
			int n2 = isExist(filesystem, newfilename);

			// Check if file not exists in the system. If so, print error     
			if (n1 < 0)
			{
				Console.WriteLine("file does not exist");
				System.Environment.Exit(1);

			}
			// Check if there is already file with the new name  
			else if (n2 >= 0)
			{
				Console.WriteLine("file " + newfilename + " already exists");
				System.Environment.Exit(1);

			}
			else
			{
				long filenameaddress = SaerchInText(contentOfSystem, filename);
				ArrayList header = getHeader(filesystem, contentOfSystem);
				//fine the file in the header & change it's name
				FileInfo node = new FileInfo();
				for (int i = 0; i < header.Count; i++)
				{
					FileInfo tempFile = (FileInfo)header[i];
					if (tempFile.getExtention().Equals("link"))
					{
						if (tempFile.getLinkName().Equals(filename))
						{
							node = tempFile;
							node.setLinkFileName(newfilename);
							break;
						}
					}
					else
					{
						if (filename.Equals(tempFile.getFileName()))
						{
							node = tempFile;
							node.setFileName(newfilename);
							break;
						}
					}
				}

				if (filename.Length == newfilename.Length)
				{
					using (StreamWriter sw = new StreamWriter(File.Open(filesystem, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
					{
						sw.BaseStream.Seek(filenameaddress, SeekOrigin.Begin);
						sw.Write(newfilename);
						sw.Close();
					}
				}
				else if (filename.Length < newfilename.Length)
				{
					long gapToMove = newfilename.Length - filename.Length;
					string sbStr = contentOfSystem.ToString(); //string of content (head of file system)
					int filesCounter = 0; //how many files we have in the system
					int indexEnd = SaerchInText(contentOfSystem, "EndOfHeader");
					int indexStart = SaerchInText(contentOfSystem, "StartOfFiles");
					int indexEndFiles = SaerchInText(contentOfSystem, "EndOfFiles");
					int indexSign = SaerchInText(contentOfSystem, "$");
					int size = (indexStart - indexEnd - 12 - 1);
					string val = sbStr.Substring(indexEnd + 12, size);//end of header section
					long endHeader = Convert.ToInt64(val);//end of header section

					size = (indexEndFiles - indexStart - 13 - 1);
					val = sbStr.Substring(indexStart + 13, size);//end of header section
					long baseFilesAdress = Convert.ToInt64(val);//end of header section

					size = (indexSign - indexEndFiles - 11);
					val = sbStr.Substring(indexEndFiles + 11, size);//end of header section
					long endFilesAdress = Convert.ToInt64(val);//end of header section

					//save length of old files of the first line
					int LastFilesNumberLen = filesCounter.ToString().Length;
					int LastEndHeaderLen = endHeader.ToString().Length;
					int LastBaseFilesLen = baseFilesAdress.ToString().Length;
					int LastEndFilesLen = endFilesAdress.ToString().Length;

					//update header
					string newFileMetaDate = node.infoFileToString(node.getExtention());
					newFileMetaDate += "\n";
					if (endHeader > (baseFilesAdress - newFileMetaDate.Length - endFilesAdress.ToString().Length)) //make sure we have space for updates to
					{
						FileInfo firstFile = firstAddressNode(filesystem, header); //returns the node in header that that starts first
						addTextToEnd(firstFile, endFilesAdress, baseFilesAdress, endHeader, filesystem, contentOfSystem);
					}
					string str = headerToString(header);
					//now we have space for the new string in the header section

					endHeader = endHeader + gapToMove; //update the end of the header

					//update first line
					//check if the new number is longer then last number in all of those fields
					int NewFilesNumberLen = filesCounter.ToString().Length;
					int NewEndHeaderLen = endHeader.ToString().Length;
					int NewBaseFilesLen = baseFilesAdress.ToString().Length;
					int NewEndFilesLen = endFilesAdress.ToString().Length;

					int difFilesNumberLen = NewFilesNumberLen - LastFilesNumberLen;
					int difEndHeaderLen = NewEndHeaderLen - LastEndHeaderLen;
					int difBaseFilesLen = NewBaseFilesLen - LastBaseFilesLen;
					int difEndFilesLen = NewEndFilesLen - LastEndFilesLen;

					//check how many chars we need to add for this update
					int howMuchMore = (difFilesNumberLen + difEndHeaderLen + difBaseFilesLen + difEndFilesLen);
					if ((howMuchMore > 0) && ((endHeader + howMuchMore) > baseFilesAdress))
					{
						FileInfo firstFile = firstAddressNode(filesystem, header); //returns the node in header that that starts first
						addTextToEnd(firstFile, endFilesAdress, baseFilesAdress, endHeader, filesystem, contentOfSystem);
					}
					//need to move the header for this changes
					MoveTextForward(filesystem, 0, endHeader, howMuchMore);
					//Now we have space for this string in the header section in generally
					endHeader += howMuchMore;
					//after this function - we have the space for this row
					string newHeadLine = "BGUFS_" + header.Count + ":,EndOfHeader:" + endHeader + ",StartOfFiles:" + baseFilesAdress + ",EndOfFiles:" + endFilesAdress + "$";
					WriteTextToIndex(filesystem, 0, newHeadLine);


					using (StreamWriter sw = new StreamWriter(File.Open(filesystem, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
					{
						sw.BaseStream.Seek(newHeadLine.Length + 1, SeekOrigin.Begin);
						sw.Write(headerToString(header));
						sw.Close();
					}

				}
				else if (filename.Length > newfilename.Length) //ifthe new name is smaller the the origin name, than we wiil overrwirte the origin name, but keep it on the same lenght by adding spaces
				{
					int gap = filename.Length - newfilename.Length;
					for (int i = 0; i < gap; i++)
					{
						newfilename = newfilename + ' ';
					}
					using (StreamWriter sw = new StreamWriter(File.Open(filesystem, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
					{
						sw.BaseStream.Seek(filenameaddress, SeekOrigin.Begin);
						sw.Write(newfilename);
						sw.Close();
					}
				}

				Boolean isLinke = node.isAlink();
				//rename all the link of this file
				if (!isLinke)
				{
					for (int i = 0; i < header.Count; i++)
					{
						FileInfo tempFile = (FileInfo)header[i];
						if (tempFile.getExtention().Equals("link"))
						{
							if (tempFile.getFileName().Equals(filename))
							{
								renameLink(filesystem, tempFile.getLinkName(), newfilename);
							}
						}
					}
				}
			}

		}


		public static void extract(string filesystem, string filename, string extractedfilename)
		{
			//string destination = Path.GetFileName(destfullpath);
			if (!File.Exists(filesystem)) //check file system exist
			{
				Console.WriteLine("File System does not exist!");
				System.Environment.Exit(1);
			}

			long FileSystemSize = 0;
			StringBuilder contentOfSystem = new StringBuilder(); ;
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				FileSystemSize = sr.BaseStream.Length; //find the length of the text
				if (FileSystemSize < 6)
				{ //if file is empty (first insertion)
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
				var buffer = new Char[Math.Min(FileSystemSize, contentOfSystem.MaxCapacity)];
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				contentOfSystem.Append(buffer);
				string checkBegin = "";
				for (int i = 0; i < 6; i++)
				{
					checkBegin += buffer[i];
				}
				if (!checkBegin.Equals("BGUFS_"))
				{
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}

			}

			int n1 = 0;
			n1 = isExist(filesystem, filename);

			if (n1 < 0)
			{

				Console.WriteLine("file does not exist");
				System.Environment.Exit(1);

			}

			else if (n1 >= 0)
			{
				ArrayList header = getHeader(filesystem, contentOfSystem);
				string detailes = ((FileInfo)header[n1]).infoFileToString(((FileInfo)header[n1]).getExtention());
				string[] detailesFields = detailes.Split(',');

				string d = detailesFields[detailesFields.Length - 1];
				d = d.Substring(0, d.Length - 1);
				int count = Convert.ToInt32(detailesFields[1]);

				char[] readChar = new Char[count];
				long num = Convert.ToInt32(d);
				using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
				{
					for (int i = 0; i < num; i++)
					{
						sr.Read();
					}
					var counter = sr.Read(readChar, 0, count);
				}

				string readString = new string(readChar);
				Byte[] bytes = Convert.FromBase64String(readString);


				File.Open(extractedfilename, FileMode.Create).Close();
				File.WriteAllBytes(extractedfilename, bytes);
			}
			else
			{
				string Text = "";
				using (StreamReader sr = new StreamReader(filename))
				{
					Text = sr.ReadToEnd();
				}
				File.WriteAllText(extractedfilename, Text);
			}
		}



		public static void dir(string filesystem) 
		{

			if (!File.Exists(filesystem)) //check file system exist
			{
				Console.WriteLine("File System does not exist!");
				System.Environment.Exit(1);
			}

			long FileSystemSize = 0;
			StringBuilder contentOfSystem = new StringBuilder(); ;
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				FileSystemSize = sr.BaseStream.Length; //find the length of the text
				if (FileSystemSize < 6)
				{ //if file is empty (first insertion)
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
				var buffer = new Char[Math.Min(FileSystemSize, contentOfSystem.MaxCapacity)];
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				contentOfSystem.Append(buffer);
				string checkBegin = "";
				for (int i = 0; i < 6; i++)
				{
					checkBegin += buffer[i];
				}
				if (!checkBegin.Equals("BGUFS_"))
				{
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
			}
			ArrayList header = getHeader(filesystem, contentOfSystem);
			if (header.Count == 0)
			{ 
				System.Environment.Exit(1);
			}

			for (int i = 0; i < header.Count; i++)
			{
				string fileName = "";
				string fileSize = "";
				string fileDOB = "";
				string fileType = "";
				string fileAddress = "";
				string fileLinkName = "";

				FileInfo node = ((FileInfo)header[i]);
				foreach (char c in node.getFileName())
				{
					if (c == ' ')
					{
						break;
					}
					else
					{
						fileName += c;
					}
				}
				foreach (char c in node.getFileSize())
				{
					if (c == ' ')
					{
						break;
					}
					else
					{
						fileSize += c;
					}
				}
				fileDOB = node.getFileDOB();
				foreach (char c in node.getFileAddress().ToString())
				{
					if (c == ' ')
						break;
					else
						fileAddress += c;
				}
				foreach (char c in node.getExtention())
				{
					if (c == ' ')
						break;
					else
						fileType += c;
				}
				if (fileType == "link")
				{
					foreach (char c in node.getLinkName())
					{
						if (c == ' ')
							break;
						else
							fileLinkName += c;
					}
					node.setLinkFileName(fileLinkName);

				}
				node.setFileName(fileName);
				node.setFileSize(fileSize);
				node.setFileDOB(fileDOB);
				node.setFileAddress(0);
				node.setfileExtension(fileType);
			}



			string final2 = "";
			for (int i = 0; i < header.Count; i++)
			{
				FileInfo temp = ((FileInfo)header[i]);
				if (temp.getExtention() == "link")
					final2 += temp.getLinkName() + "," + temp.getFileSize() + "," + temp.getFileDOB() + ",link," + temp.getFileName();
				else
					final2 += temp.getFileName() + "," + temp.getFileSize() + "," + temp.getFileDOB() + ",regular";
				if (i != header.Count - 1)
				{
					final2 += "\n";
				}
			}

			Console.WriteLine(final2);
		}


		static void hash(string filesystem, string filename) 
		{

			if (!File.Exists(filesystem)) //check file system exist
			{
				Console.WriteLine("File System does not exist!");
				System.Environment.Exit(1);
			}

			//make sure this file system have the right forams - BGUFS_
			StringBuilder systemContent = new StringBuilder();
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				long FileSystemSize = sr.BaseStream.Length; //find the length of the text
				var buffer = new Char[Math.Min(FileSystemSize, systemContent.MaxCapacity)];  //create new buffer of 6 chars
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				string checkBegin = "";
				for (int i = 0; i < 6; i++)
				{
					checkBegin += buffer[i];
				}
				if (!checkBegin.Equals("BGUFS_"))
				{
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
				systemContent.Append(buffer);
			}

			int n1 = isExist(filesystem, filename);
			// Check if file not exists in the system. If so, print error     
			if (n1 < 0)
			{
				Console.WriteLine("file does not exist");
				System.Environment.Exit(1);
			}

			//find this file meta data (look for it's row in the header), after check it's in the system
			ArrayList header = getHeader(filesystem, systemContent);
			if (header.Count == 0)
			{ 
				System.Environment.Exit(1);
			}
			FileInfo file = null;
			for (int i = 0; i < header.Count; i++)
			{
				FileInfo tempFile = (FileInfo)header[i];
				if (tempFile.getExtention().Equals("link"))
				{
					if (tempFile.getLinkName().Equals(filename))
					{
						file = tempFile;
						break;
					}
				}
				else
				{
					if (filename.Equals(tempFile.getFileName()))
					{
						file = tempFile;
						break;
					}
				}
			}
			if (file == null)
			{
				Console.WriteLine("file does not exist");
				System.Environment.Exit(1);
			}
			if (file.isAlink())
			{ //if the given file is a link -> find the source filef
				string theFileItself = file.getFileName();
				for (int i = 0; i < header.Count; i++)
				{
					FileInfo tempFile = (FileInfo)header[i];
					if ((!(tempFile.getExtention().Equals("link"))) && (theFileItself.Equals(tempFile.getFileName())))
					{
						file = tempFile;
						break;
					}
				}
			}
			//extract file's start & end indexes
			long startIndex = file.getFileAddress();
			long endIndex = Convert.ToInt64(file.getFileSize()) + startIndex - 1;

			//read file content
			StringBuilder FileContent = new StringBuilder();
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				sr.BaseStream.Seek(startIndex, SeekOrigin.Begin);
				var buffer = new Char[endIndex - startIndex + 1];
				sr.Read(buffer, 0, buffer.Length);
				FileContent.Append(buffer);
			}

			string Data = FileContent.ToString();
			byte[] Hash;

			//Create a byte array from source data.
			Byte[] Source = Convert.FromBase64String(Data);
			Hash = new MD5CryptoServiceProvider().ComputeHash(Source);
			Console.WriteLine(String.Join("", Hash));
		}


		static void optimize(string filesystem)
		{
			if (!File.Exists(filesystem)) //check file system exist
			{
				Console.WriteLine("File System does not exist!");
				System.Environment.Exit(1);
			}

			//make sure this file system have the right forams - BGUFS_
			StringBuilder systemContent = new StringBuilder();
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				long FileSystemSize = sr.BaseStream.Length; //find the length of the text
				var buffer = new Char[Math.Min(FileSystemSize, systemContent.MaxCapacity)];  //create new buffer of 6 chars
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				string checkBegin = "";
				for (int i = 0; i < 6; i++)
				{
					checkBegin += buffer[i];
				}
				if (!checkBegin.Equals("BGUFS_"))
				{
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
				systemContent.Append(buffer);
			}
		}


		public static void sortAB(string filesystem) 
		{
			if (!File.Exists(filesystem)) //check file system exist
			{
				Console.WriteLine("File System does not exist!");
				System.Environment.Exit(1);
			}

			int addressToStartFrom = 0;

			long FileSystemSize = 0;
			StringBuilder contentOfSystem = new StringBuilder(); ;
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				FileSystemSize = sr.BaseStream.Length; //find the length of the text
				if (FileSystemSize < 6)
				{ //if file is empty (first insertion)
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
				var buffer = new Char[Math.Min(FileSystemSize, contentOfSystem.MaxCapacity)];
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				contentOfSystem.Append(buffer);
				string checkBegin = "";
				for (int i = 0; i < 6; i++)
				{
					checkBegin += buffer[i];
				}
				if (!checkBegin.Equals("BGUFS_"))
				{
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
			}
			ArrayList header = getHeader(filesystem, contentOfSystem);
			if (header.Count == 0) { 
				System.Environment.Exit(1);
			}

			string firstFirstHeaderName;
			if (((FileInfo)header[0]).getExtention().Equals("link"))
			{
				firstFirstHeaderName = ((FileInfo)header[0]).getLinkName();
			}
			else
			{
				firstFirstHeaderName = ((FileInfo)header[0]).getFileName();
			}

			addressToStartFrom = SaerchInText(contentOfSystem, firstFirstHeaderName);

			using (StreamWriter sw = new StreamWriter(File.Open(filesystem, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
			{
				sw.BaseStream.Seek(addressToStartFrom, SeekOrigin.Begin);
				header.Sort(new FileComparerAB());
				sw.WriteLine(headerToString(header));
				sw.Close();
			}
		}

		public static void sortDate(string filesystem) 
		{
			if (!File.Exists(filesystem)) //check file system exist
			{
				Console.WriteLine("File System does not exist!");
				System.Environment.Exit(1);
			}

			long FileSystemSize = 0;
			StringBuilder contentOfSystem = new StringBuilder(); ;
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				FileSystemSize = sr.BaseStream.Length; //find the length of the text
				if (FileSystemSize < 6)
				{ //if file is empty (first insertion)
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
				var buffer = new Char[Math.Min(FileSystemSize, contentOfSystem.MaxCapacity)];
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				contentOfSystem.Append(buffer);
				string checkBegin = "";
				for (int i = 0; i < 6; i++)
				{
					checkBegin += buffer[i];
				}
				if (!checkBegin.Equals("BGUFS_"))
				{
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
			}
			int addressToStartFrom = 0;
			ArrayList header = getHeader(filesystem, contentOfSystem);
			if (header.Count == 0)
			{ 
				System.Environment.Exit(1);
			}

			string firstFirstHeaderName;
			if (((FileInfo)header[0]).getExtention().Equals("link"))
			{
				firstFirstHeaderName = ((FileInfo)header[0]).getLinkName();
			}
			else
			{
				firstFirstHeaderName = ((FileInfo)header[0]).getFileName();
			}

			addressToStartFrom = SaerchInText(contentOfSystem, firstFirstHeaderName);

			using (StreamWriter sw = new StreamWriter(File.Open(filesystem, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
			{
				sw.BaseStream.Seek(addressToStartFrom, SeekOrigin.Begin);
				header.Sort(new FileComparerAB()); //internal sort by AB (if two files have the same size, sort by AB)
				header.Sort(new FileComparerDate());
				sw.WriteLine(headerToString(header));
				sw.Close();
			}

		}


		public static void sortSize(string filesystem) 
		{
			if (!File.Exists(filesystem)) //check file system exist
			{
				Console.WriteLine("File System does not exist!");
				System.Environment.Exit(1);
			}

			int addressToStartFrom = 0;

			long FileSystemSize = 0;
			StringBuilder contentOfSystem = new StringBuilder(); ;
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				FileSystemSize = sr.BaseStream.Length; //find the length of the text
				if (FileSystemSize < 6)
				{ //if file is empty (first insertion)
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
				var buffer = new Char[Math.Min(FileSystemSize, contentOfSystem.MaxCapacity)];
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				contentOfSystem.Append(buffer);
				string checkBegin = "";
				for (int i = 0; i < 6; i++)
				{
					checkBegin += buffer[i];
				}
				if (!checkBegin.Equals("BGUFS_"))
				{
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
			}
			ArrayList header = getHeader(filesystem, contentOfSystem);
			if (header.Count == 0)
			{ 
				System.Environment.Exit(1);
			}

			string firstFirstHeaderName;
			if (((FileInfo)header[0]).getExtention().Equals("link"))
			{
				firstFirstHeaderName = ((FileInfo)header[0]).getLinkName();
			}
			else
			{
				firstFirstHeaderName = ((FileInfo)header[0]).getFileName();
			}

			addressToStartFrom = SaerchInText(contentOfSystem, firstFirstHeaderName);


			using (StreamWriter sw = new StreamWriter(File.Open(filesystem, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
			{
				sw.BaseStream.Seek(addressToStartFrom, SeekOrigin.Begin);
				header.Sort(new FileComparerAB()); //internal sort by AB (if two files have the same size, sort by AB)
				header.Sort(new FileComparerSize());
				sw.WriteLine(headerToString(header));
				sw.Close();
			}
		}


		public static void addLink(string filesystem, string linkfilename, string existingfilename)
		{

			if (!File.Exists(filesystem)) //check file system exist
			{
				Console.WriteLine("File System does not exist!");
				System.Environment.Exit(1);
			}

			int n1;
			n1 = isExist(filesystem, existingfilename);

			if (n1 < 0)
			{
				Console.WriteLine("file does not exist");
				System.Environment.Exit(1);
			}

			int n2;
			n2 = isExist(filesystem, linkfilename);
			if (n2 >= 0)
			{
				Console.WriteLine("file already exist");
				System.Environment.Exit(1);
			}

			StringBuilder contentOfSystem = new StringBuilder();
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				long FileSystemSize = sr.BaseStream.Length; //find the length of the text
				var buffer = new Char[Math.Min(FileSystemSize, contentOfSystem.MaxCapacity)];  //create new buffer of 6 chars
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				string checkBegin = "";
				for (int i = 0; i < 6; i++)
				{
					checkBegin += buffer[i];
				}
				if (!checkBegin.Equals("BGUFS_"))
				{
					Console.WriteLine("Not a BGUFS file");
					System.Environment.Exit(1);
				}
				contentOfSystem.Append(buffer);
			}

			ArrayList header = getHeader(filesystem, contentOfSystem);
			FileInfo newNode = new FileInfo();
			for (int i = 0; i < header.Count; i++)
			{
				if (((FileInfo)header[i]).getFileName() == existingfilename)
				{
					newNode = (FileInfo)header[i];
					break;
				}
			}

			FileInfo linkeNode = new FileInfo(newNode, linkfilename);

			string Text = linkeNode.infoFileToString(linkeNode.getExtention());

			//add To header, update number of files, check if there is more space for the new line.
			string sbStr = contentOfSystem.ToString();

			int indexEnd = SaerchInText(contentOfSystem, "EndOfHeader");
			int indexStart = SaerchInText(contentOfSystem, "StartOfFiles");
			int indexEndFiles = SaerchInText(contentOfSystem, "EndOfFiles");
			int indexSign = SaerchInText(contentOfSystem, "$");
			int size = (indexStart - indexEnd - 12 - 1);
			string val = sbStr.Substring(indexEnd + 12, size);//end of header section
			long endHeader = Convert.ToInt64(val);//end of header section

			size = (indexEndFiles - indexStart - 13 - 1);
			val = sbStr.Substring(indexStart + 13, size);//end of header section
			long baseFilesAdress = Convert.ToInt64(val);//end of header section

			size = (indexSign - indexEndFiles - 11);
			val = sbStr.Substring(indexEndFiles + 11, size);//end of header section
			long endFilesAdress = Convert.ToInt64(val);//end of header section

			//add the new file to the header
			header.Add(linkeNode);

			//save length of old files of the first line
			int LastFilesNumberLen = header.Count.ToString().Length;
			int LastEndHeaderLen = endHeader.ToString().Length;
			int LastBaseFilesLen = baseFilesAdress.ToString().Length;
			int LastEndFilesLen = endFilesAdress.ToString().Length;


			//update header
			string newFileMetaDate = linkeNode.infoFileToString("link");
			newFileMetaDate += "\n";
			if (endHeader > (baseFilesAdress - newFileMetaDate.Length - endFilesAdress.ToString().Length)) //make sure we have space for updates to
			{
				FileInfo firstFile = firstAddressNode(filesystem, header); //returns the node in header that that starts first
				addTextToEnd(firstFile, endFilesAdress, baseFilesAdress, endHeader, filesystem, contentOfSystem);
			}

			//now we have space for the new string in the header section
			WriteTextToIndex(filesystem, endHeader, newFileMetaDate);

			endHeader = endHeader + newFileMetaDate.Length; //update the end of the header
			int filesCounter = header.Count; //now we have an extra file in the system

			//update first line
			//check if the new number is longer then last number in all of those fields
			int NewFilesNumberLen = filesCounter.ToString().Length;
			int NewEndHeaderLen = endHeader.ToString().Length;
			int NewBaseFilesLen = baseFilesAdress.ToString().Length;
			int NewEndFilesLen = endFilesAdress.ToString().Length;

			int difFilesNumberLen = NewFilesNumberLen - LastFilesNumberLen;
			int difEndHeaderLen = NewEndHeaderLen - LastEndHeaderLen;
			int difBaseFilesLen = NewBaseFilesLen - LastBaseFilesLen;
			int difEndFilesLen = NewEndFilesLen - LastEndFilesLen;

			//check how many chars we need to add for this update
			int howMuchMore = (difFilesNumberLen + difEndHeaderLen + difBaseFilesLen + difEndFilesLen);
			if ((howMuchMore > 0) && ((endHeader + howMuchMore) > baseFilesAdress))
			{
				FileInfo firstFile = firstAddressNode(filesystem, header); //returns the node in header that that starts first
				addTextToEnd(firstFile, endFilesAdress, baseFilesAdress, endHeader, filesystem, contentOfSystem);
			}
			//need to move the header for this changes
			MoveTextForward(filesystem, 0, endHeader, howMuchMore);
			//Now we have space for this string in the header section in generally
			endHeader += howMuchMore;
			//after this function - we have the space for this row
			string newHeadLine = "BGUFS_" + filesCounter + ":,EndOfHeader:" + endHeader + ",StartOfFiles:" + baseFilesAdress + ",EndOfFiles:" + endFilesAdress + "$";
			WriteTextToIndex(filesystem, 0, newHeadLine);
		}




		// ---------------------------------------- helper funcions ----------------------------------------

		//rename link file, after the pointed file name has changed
		static void renameLink(string filesystem, string linkName, string newfilename)
		{
			//make sure this file system have the right forams - BGUFS_
			long FileSystemSize = 0;
			StringBuilder contentOfSystem = new StringBuilder(); ;
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				FileSystemSize = sr.BaseStream.Length; //find the length of the text
				var buffer = new Char[Math.Min(FileSystemSize, contentOfSystem.MaxCapacity)];  //create new buffer of 6 chars
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				contentOfSystem.Append(buffer);
			}
			int n1 = isExist(filesystem, linkName);
			int n2 = isExist(filesystem, newfilename);
			// Check if file not exists. If so, print error     
			if (!File.Exists(linkName) && n1 < 0)
			{
				Console.WriteLine("file does not exist");
				System.Environment.Exit(1);

			}
			else
			{
				ArrayList header = getHeader(filesystem, contentOfSystem);
				if (header.Count == 0)
				{ 
					System.Environment.Exit(1);
				}
				string oldFileName = "";
				//fine the link in the header & change it's file name
				FileInfo node = new FileInfo();
				for (int i = 0; i < header.Count; i++)
				{
					FileInfo tempFile = (FileInfo)header[i];
					if (tempFile.getExtention().Equals("link"))
					{
						if (tempFile.getLinkName().Equals(linkName))
						{
							node = tempFile;
							oldFileName = node.getFileName();
							break;
						}
					}
				}
				//find the index of the name we want to change
				long filenameaddress = SaerchInText(contentOfSystem, linkName);
				string meta = node.infoFileToString(node.getExtention());
				StringBuilder sb = new StringBuilder();
				sb.Append(meta);
				int indexIn = SaerchInText(sb, oldFileName);
				filenameaddress += indexIn;
				node.setFileName(newfilename);

				if (oldFileName.Length == newfilename.Length)
				{
					using (StreamWriter sw = new StreamWriter(File.Open(filesystem, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
					{
						sw.BaseStream.Seek(filenameaddress, SeekOrigin.Begin);
						sw.Write(newfilename);
						sw.Close();
					}
				}
				else if (oldFileName.Length < newfilename.Length)
				{
					long gapToMove = newfilename.Length - oldFileName.Length;
					string sbStr = contentOfSystem.ToString(); //string of content (head of file system)
					int filesCounter = 0; //how many files we have in the system
					int indexEnd = SaerchInText(contentOfSystem, "EndOfHeader");
					int indexStart = SaerchInText(contentOfSystem, "StartOfFiles");
					int indexEndFiles = SaerchInText(contentOfSystem, "EndOfFiles");
					int indexSign = SaerchInText(contentOfSystem, "$");
					int size = (indexStart - indexEnd - 12 - 1);
					string val = sbStr.Substring(indexEnd + 12, size);//end of header section
					long endHeader = Convert.ToInt64(val);//end of header section

					size = (indexEndFiles - indexStart - 13 - 1);
					val = sbStr.Substring(indexStart + 13, size);//end of header section
					long baseFilesAdress = Convert.ToInt64(val);//end of header section

					size = (indexSign - indexEndFiles - 11);
					val = sbStr.Substring(indexEndFiles + 11, size);//end of header section
					long endFilesAdress = Convert.ToInt64(val);//end of header section

					//save length of old files of the first line
					int LastFilesNumberLen = filesCounter.ToString().Length;
					int LastEndHeaderLen = endHeader.ToString().Length;
					int LastBaseFilesLen = baseFilesAdress.ToString().Length;
					int LastEndFilesLen = endFilesAdress.ToString().Length;

					//update header
					string newFileMetaDate = node.infoFileToString(node.getExtention());
					newFileMetaDate += "\n";
					if (endHeader > (baseFilesAdress - newFileMetaDate.Length - endFilesAdress.ToString().Length)) //make sure we have space for updates to
					{
						FileInfo firstFile = firstAddressNode(filesystem, header); //returns the node in header that that starts first
						addTextToEnd(firstFile, endFilesAdress, baseFilesAdress, endHeader, filesystem, contentOfSystem);
					}
					string str = headerToString(header);
					//now we have space for the new string in the header section

					endHeader = endHeader + gapToMove; //update the end of the header

					//update first line
					//check if the new number is longer then last number in all of those fields
					int NewFilesNumberLen = filesCounter.ToString().Length;
					int NewEndHeaderLen = endHeader.ToString().Length;
					int NewBaseFilesLen = baseFilesAdress.ToString().Length;
					int NewEndFilesLen = endFilesAdress.ToString().Length;

					int difFilesNumberLen = NewFilesNumberLen - LastFilesNumberLen;
					int difEndHeaderLen = NewEndHeaderLen - LastEndHeaderLen;
					int difBaseFilesLen = NewBaseFilesLen - LastBaseFilesLen;
					int difEndFilesLen = NewEndFilesLen - LastEndFilesLen;

					//check how many chars we need to add for this update
					int howMuchMore = (difFilesNumberLen + difEndHeaderLen + difBaseFilesLen + difEndFilesLen);
					if ((howMuchMore > 0) && ((endHeader + howMuchMore) > baseFilesAdress))
					{
						FileInfo firstFile = firstAddressNode(filesystem, header); //returns the node in header that that starts first
						addTextToEnd(firstFile, endFilesAdress, baseFilesAdress, endHeader, filesystem, contentOfSystem);
					}
					//need to move the header for this changes
					MoveTextForward(filesystem, 0, endHeader, howMuchMore);
					//Now we have space for this string in the header section in generally
					endHeader += howMuchMore;
					//after this function - we have the space for this row
					string newHeadLine = "BGUFS_" + header.Count + ":,EndOfHeader:" + endHeader + ",StartOfFiles:" + baseFilesAdress + ",EndOfFiles:" + endFilesAdress + "$";
					WriteTextToIndex(filesystem, 0, newHeadLine);


					using (StreamWriter sw = new StreamWriter(File.Open(filesystem, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
					{
						sw.BaseStream.Seek(newHeadLine.Length + 1, SeekOrigin.Begin);
						sw.Write(headerToString(header));
						sw.Close();
					}

				}
				else if (oldFileName.Length > newfilename.Length) //ifthe new name is smaller the the origin name, than we wiil overrwirte the origin name, but keep it on the same lenght by adding spaces
				{
					int gap = oldFileName.Length - newfilename.Length;
					for (int i = 0; i < gap; i++)
					{
						newfilename = newfilename + ' ';
					}
					using (StreamWriter sw = new StreamWriter(File.Open(filesystem, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
					{
						sw.BaseStream.Seek(filenameaddress, SeekOrigin.Begin);
						sw.Write(newfilename);
						sw.Close();
					}
				}
			}

		}


		//function that returns all the header as string
		public static string headerToString(ArrayList header)
		{
			string headerToString = "";
			for (int i = 0; i < header.Count; i++)
			{
				if (i == header.Count - 1)
					headerToString += ((FileInfo)header[i]).infoFileToString(((FileInfo)header[i]).getExtention());
				else
					headerToString += ((FileInfo)header[i]).infoFileToString(((FileInfo)header[i]).getExtention()) + "\n";


			}
			return headerToString;

		}

		public static StringBuilder checkInput(string filesystem, string filename)
		{
			if (!File.Exists(filesystem)) //check file system exist
			{
				Console.WriteLine("File System does not exist!");
				return null;
			}

			// Check if file exists.     
			if (!File.Exists(filename))
			{
				Console.WriteLine("file does not exist");
				return null;
			}

			//make sure this file system have the right forams - BGUFS_
			StringBuilder systemContent = new StringBuilder(); ;
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				long FileSystemSize = sr.BaseStream.Length; //find the length of the text
				var buffer = new Char[Math.Min(FileSystemSize, systemContent.MaxCapacity)];  //create new buffer of 6 chars
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				string checkBegin = "";
				for (int i = 0; i < 6; i++)
				{
					checkBegin += buffer[i];
				}
				if (!checkBegin.Equals("BGUFS_"))
				{
					Console.WriteLine("Not a BGUFS file");
					return null;
				}
				systemContent.Append(buffer);
			}
			return systemContent;
		}

		public static void addTextToEnd(FileInfo firstFile, long endFilesAdress, long baseFilesAdress, long endHeader, string filesystem, StringBuilder contentOfSystem)
		{
			int gap = endFilesAdress.ToString().Length - firstFile.getFileAddress().ToString().Length; //difference between last length of file adress to it's new one
			baseFilesAdress = firstFile.getFileAddress() + Convert.ToInt32(firstFile.getFileSize()); //new base files address
			writeToEnd(filesystem, firstFile, endFilesAdress); //write this file to the end of the files system
			firstFile.setFileAddress(endFilesAdress);
			//update this file meta data in header & write changes to file
			long indexOfMeta = SaerchInText(contentOfSystem, firstFile.getFileName());
			string FileMetaDate = firstFile.infoFileToString("regular");
			if (gap > 0)
			{
				MoveTextForward(filesystem, indexOfMeta + FileMetaDate.Length, endHeader, gap);
			}
			WriteTextToIndex(filesystem, indexOfMeta, FileMetaDate);
			endFilesAdress = firstFile.getFileAddress() + Convert.ToInt32(firstFile.getFileSize()); //new end files address
			endHeader += gap;
		}

		//function that writes to text in specific index
		public static void WriteTextToIndex(string filesystem, long startIndex, string text)
		{
			using (StreamWriter sw = new StreamWriter(File.Open(filesystem, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
			{
				sw.BaseStream.Seek(startIndex, SeekOrigin.Begin);
				sw.Write(text);
				sw.Close();
			}
		}

		//function that move text Backwards in gap chars, start from a given index to an end index
		public static void MoveTextBackwards(string filesystem, long startIndex, long endIndex, int gap)
		{
			char[] buffer;
			StringBuilder sb = new StringBuilder();
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				sr.BaseStream.Seek(startIndex, SeekOrigin.Begin);
				buffer = new Char[endIndex - startIndex + 1];
				sr.Read(buffer, 0, buffer.Length);
				sb.Append(buffer);
			}
			//write file to the end of the system
			WriteTextToIndex(filesystem, startIndex - gap, sb.ToString());
		}


		//function that move text forward in gap chars, start from a given index to an end index
		public static void MoveTextForward(string filesystem, long startIndex, long endIndex, int gap)
		{
			char[] buffer;
			StringBuilder sb = new StringBuilder();
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				sr.BaseStream.Seek(startIndex, SeekOrigin.Begin);
				buffer = new Char[endIndex - startIndex + 1];
				sr.Read(buffer, 0, buffer.Length);
				sb.Append(buffer);
			}
			//write file to the end of the system
			WriteTextToIndex(filesystem, startIndex + gap, sb.ToString());
		}

		//function that writes file to the end of the file system
		public static void writeToEnd(string filesystem, FileInfo firstFile, long endFilesAdress)
		{
			//write this file to the end - first - read file content
			long FileAdress = Convert.ToInt32(firstFile.getFileAddress());
			int FileSize = Convert.ToInt32(firstFile.getFileSize());
			char[] buffer;
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				sr.BaseStream.Seek(FileAdress, SeekOrigin.Begin);
				buffer = new Char[FileSize];
				sr.Read(buffer, 0, buffer.Length);
			}
			//write file to the end of the system
			WriteTextToIndex(filesystem, endFilesAdress, buffer.ToString());

		}

		//this function return the FileNode with the first address (in sorted array)
		public static FileInfo firstAddressNode(string filesystem, ArrayList header)
		{
			header.Sort(new FileInfoComparer());
			return (FileInfo)header[0];
		}

		//this function return the FileNode with the last start address (in sorted array)
		public static FileInfo LastAddressNode(string filesystem, ArrayList header)
		{
			header.Sort(new FileInfoComparer());
			return (FileInfo)header[header.Count - 1];
		}

		//this function return the FileNode before the one with the last start address (in sorted array)
		public static FileInfo almostLastAddressNode(string filesystem, ArrayList header)
		{
			header.Sort(new FileInfoComparer());
			return (FileInfo)header[header.Count - 2];
		}

		//this function check if there is already file inside the file system with this name. if it does return the address, else -1
		public static int isExist(string filesystem, string filename)
		{
			int address = -1;
			long FileSystemSize = 0;
			StringBuilder contentOfSystem = new StringBuilder(); ;
			using (var sr = new StreamReader(filesystem))  // Open the text file using a stream reader
			{
				FileSystemSize = sr.BaseStream.Length; //find the length of the text
				if (FileSystemSize < 6)
				{ //if file is empty (first insertion)
					Console.WriteLine("Not a BGUFS file");
					return -10;
				}
				var buffer = new Char[Math.Min(FileSystemSize, contentOfSystem.MaxCapacity)];
				var counter = sr.Read(buffer, 0, buffer.Length); //read sr into buffer, between indexes: 0-6. conter = how many chars we have read in each iteration
				contentOfSystem.Append(buffer);
			}
			ArrayList header = getHeader(filesystem, contentOfSystem);
			for (int i = 0; i < header.Count; i++)
			{
				FileInfo temp = (FileInfo)header[i];
				if (temp.getExtention().Equals("link"))
				{
					if (temp.getLinkName().Equals(filename))
					{
						return i;
					}
				}
				else
				{
					if (temp.getFileName().Equals(filename))
					{
						return i;
					}
				}
			}
			return address;
		}

		public static FileInfo creatNode(string line, string type)
		{
			if (type == "regular")
			{
				FileInfo node = new FileInfo();
				string[] param = new string[5];
				{
					int index = 0;
					foreach (char c in line)
					{
						if (c == ',' || c == '$')
						{
							index++;
						}
						else
						{
							param[index] += c;
						}
					}
					node.setExtention(false);
					node.setFileName(param[0]);
					node.setFileSize(param[1]);
					node.setFileDOB(param[2]);
					node.setfileExtension(param[3]);
					node.setFileAddress(Convert.ToInt32(param[4]));
					return node;
				}
			}
			else
			{
				FileInfo node = new FileInfo();
				string[] param = new string[6];
				{
					int index = 0;
					foreach (char c in line)
					{
						if (c == ',' || c == '$')
						{
							index++;
						}
						else
						{
							param[index] += c;
						}
					}
					node.setExtention(true);
					node.setLinkFileName(param[0]);
					node.setFileSize(param[1]);
					node.setFileDOB(param[2]);
					node.setfileExtension(param[3]);
					node.setFileName(param[4]);
					node.setFileAddress(Convert.ToInt32(param[5]));

					return node;
				}



			}

		}

		// this function finds the given string in the given StringBuilder, and return it's index (if exist), else - return -1
		public static int SaerchInText(StringBuilder textForSearch, string StringToSearch)
		{
			int count = 0; //will count how many optional indexes we have to check (by finding the indexes of first char in StringBuilder text)
			var IndexList = new List<int>();  //create an empty list of ints (will hold possible indexes of the wanted value)
			char FirstChar = StringToSearch[0]; //first letter of the given string
												//find all indexes of chars in the text that are equal to the first lette in StringToSearch
			for (int index = 0; index < textForSearch.Length; index++)
			{
				if (textForSearch[index] == FirstChar)
				{
					IndexList.Add(index);
					count++;
				}
			}

			if (count == 0)
			{
				return -1;
			}  //this first letter isn't it the text at all

			//iterate over all this indexes, and look for the whole string (check if it starts from this index)
			//if we have found the strign - return with it's index
			for (int i = 0; i < count; i++)
			{
				String temp = null;
				int CurrStartIntdex = IndexList[i];
				for (int j = 0; j < (StringToSearch.Length); j++)
				{
					temp += textForSearch[CurrStartIntdex + j];
				}
				if (temp.Equals(StringToSearch))
				{ //if we have find the StringToSearch - return it's index in this given text
					return (CurrStartIntdex);
				}
			}
			//if we have reached here - output was not found
			return -1;
		}

		// this function returns the names of all the files in the system (by order)
		public static ArrayList NameOfFile(StringBuilder textForSearch, int startSearch, int EndSearch)
		{
			ArrayList NamesOfFiles = new ArrayList();

			string headerString = (textForSearch.ToString()).Substring(startSearch, (EndSearch - startSearch));
			var IndexList = new List<int>();  //create an empty list of ints (will hold possible indexes of the wanted value find all indexes of $ chars (new files rows -> meta data)
			for (int index = 0; index < headerString.Length; index++)
			{
				char curr = headerString[index];
				if (curr.Equals('$'))
				{
					IndexList.Add(index + 2); //skip over one "$\""
				}
			}

			for (int index = 0; index < IndexList.Count; index++)
			{
				string temp = "";
				int currIndex = IndexList[index];
				char currChar = headerString[currIndex];
				while (currChar != '\"')
				{
					temp += currChar;
					currIndex++;
					currChar = headerString[currIndex];
				}
				NamesOfFiles.Add(temp);
			}
			return NamesOfFiles;
		}

		// this function returns the begin adresses of all the files in the system (by order)
		public static ArrayList StartsOfFileInSystem(StringBuilder textForSearch, int startSearch, int EndSearch)
		{
			ArrayList startAdresses = new ArrayList();

			string headerString = (textForSearch.ToString()).Substring(startSearch, (EndSearch - startSearch));
			var IndexList = new List<int>();  //create an empty list of ints (will hold possible indexes of the wanted value)
											  //find all indexes of b chars (new files rows -> meta data)
			for (int index = 0; index < (headerString.Length - 4); index++)
			{
				string temp = headerString.Substring(index, 4);
				if (temp.Equals("base"))
				{
					IndexList.Add(index + 6); //skip over the "base: "
				}
			}

			for (int index = 0; index < IndexList.Count; index++)
			{
				string temp = "";
				int currIndex = IndexList[index];
				char currChar = headerString[currIndex];
				while (currChar != ',')
				{
					temp += currChar;
					currIndex++;
					currChar = headerString[currIndex];
				}
				startAdresses.Add(Convert.ToInt32(temp));
			}
			return startAdresses;
		}

		// this function returns the Length of all the files in the system (by order)
		public static ArrayList LengthOfFileInSystem(StringBuilder textForSearch, int startSearch, int EndSearch)
		{
			ArrayList LengthOfAll = new ArrayList();

			string headerString = (textForSearch.ToString()).Substring(startSearch, (EndSearch - startSearch));
			var IndexList = new List<int>();  //create an empty list of ints (will hold possible indexes of the wanted value)
											  //find all indexes of b chars (new files rows -> meta data)
			for (int index = 0; index < (headerString.Length - 6); index++)
			{
				string temp = headerString.Substring(index, 6);
				if (temp.Equals("length"))
				{
					IndexList.Add(index + 7); //skip over the "length:"
				}
			}

			for (int index = 0; index < IndexList.Count; index++)
			{
				string temp = "";
				int currIndex = IndexList[index];
				char currChar = headerString[currIndex];
				while ((currChar != '\n') && (currChar != '\r'))
				{
					temp += currChar;
					currIndex++;
					currChar = headerString[currIndex];
				}
				LengthOfAll.Add(Convert.ToInt32(temp));
			}
			return LengthOfAll;
		}

		// this function returns the lines of all header's files (by order)
		public static ArrayList HeaderFilesToList(StringBuilder textForSearch, int startSearch, int EndSearch)
		{
			ArrayList allFiles = new ArrayList();

			string headerString = (textForSearch.ToString()).Substring(startSearch, (EndSearch - startSearch));
			var IndexList = new List<int>();  //create an empty list of ints (will hold possible indexes of the wanted value)
			for (int index = 0; index < headerString.Length; index++)
			{
				char curr = headerString[index];
				if (curr.Equals('$'))
				{
					IndexList.Add(index); //skip over one "$"
				}
			}

			for (int index = 0; index < IndexList.Count; index++)
			{
				string temp = "";
				int currIndex = IndexList[index];
				char currChar = headerString[currIndex];
				while ((currChar != '\n') && (currChar != '\r'))
				{
					temp += currChar;
					currIndex++;
					currChar = headerString[currIndex];
				}
				temp += "\n";
				allFiles.Add(temp);
			}
			return allFiles;
		}

		//this function get the fileSystem and returns the heaedr
		public static ArrayList getHeader(string filesystem, StringBuilder contentOfSystem)
		{
			int filesCounter = 0; //how many files we have in the system
			ArrayList header = new ArrayList();
			//build the header lines list
			int dilimtFirst = SaerchInText(contentOfSystem, ":");
			string sbStr = contentOfSystem.ToString();
			string filesCounterStr = sbStr.Substring(6, dilimtFirst - 6);
			filesCounter = Convert.ToInt32(filesCounterStr);
			int tempCounter = filesCounter;
			string strToSearch = sbStr.Substring(sbStr.IndexOf('$') + 1);
			string[] lines = strToSearch.Split('$');
			foreach (string line in lines)
			{
				if (tempCounter == 0)
					break;
				else
				{
					string temp = "";
					string type = "";
					for (int i = 0; i < line.Length; i++)
					{
						if ((line[i] != '\n') && (line[i] != '\r'))
						{
							temp += line[i];
						}
					}
					char fieldsnumber = ','; //counting the number of feilds in order to detrmine the file type
					int freq = temp.Count(f => (f == fieldsnumber));
					if (freq == 4)
						type = "regular";
					else if (freq == 5)
						type = "link";
					FileInfo newNode = creatNode(temp, type);
					header.Add(newNode);
					tempCounter--;
				}
			}

			return header;
		}

		//this function get a size of file to insert and check if there is a gap to insert it in. if not, return -1
		public static long insertInGap(string filesystem, int fileSize, StringBuilder content)
		{
			ArrayList header = getHeader(filesystem, content);
			header.Sort(new FileInfoComparer());

			for (int i = 0; i < header.Count - 1; i++)
			{

				long size = Convert.ToInt64(((FileInfo)header[i]).getFileSize());

				if (((FileInfo)header[i + 1]).getFileAddress() - (((FileInfo)header[i]).getFileAddress() + size) >= fileSize)
				{
					return ((FileInfo)header[i]).getFileAddress() + size;
				}

			}

			return -1;
		}

	}

}
