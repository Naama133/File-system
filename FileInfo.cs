using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BGUFS
{
	class FileInfo
	{
		public string _fileName;
		public string _fileSize;
		public string _fileDOB;
		public long _address;
		public bool isLink;
		public string _linkName;
		public FileInfo(string fileName)
		{
			this._fileName = new System.IO.FileInfo(fileName).Name;
			this._fileDOB = new System.IO.FileInfo(fileName).CreationTime.ToString();
		}

		public FileInfo(FileInfo file, string linkName)
		{

			this._linkName = linkName;
			this._fileName = file.getFileName();
			this._fileDOB = file.getFileDOB();
			this._fileSize = file.getFileSize();
			this._address = file.getFileAddress();
			this.isLink = true;
		}

		public void setExtention(bool s)
		{
			this.isLink = s;
		}

		public FileInfo()
		{
			this._fileName = "";
			this._fileDOB = "";
			this._fileSize = "";
		}

		public void setLinkFileName(string s)
		{
			this._linkName = s;

		}

		public void setFileName(string s)
		{
			this._fileName = s;
		}
		public void setFileSize(string s)
		{
			this._fileSize = s;
		}
		public void setFileDOB(string s)
		{
			this._fileDOB = s;
		}
		public void setfileExtension(string s)
		{
			if (s == "regular")
				this.isLink = false;
			else if (s == "link")
				this.isLink = true;
		}

		public string getLinkName()
		{
			return this._linkName;
		}

		public string getFileSize()
		{
			return this._fileSize;
		}
		public string getFileName()
		{
			return this._fileName;
		}
		public string getFileDOB()
		{
			return String.Format("{0:d/M/yyyy HH:mm:ss}", this._fileDOB);
		}
		public string getExtention()
		{
			if (this.isLink == true)
				return "link";
			else
				return "regular";
		}
		public Boolean isAlink()
		{
			return this.isLink;
		}
		public long getFileAddress()
		{
			return this._address;
		}
		public void setFileAddress(long address)
		{
			this._address = address;
		}

		public string infoFileToString(string type)
		{
			string str = "";
			if (type == "regular")
				str += this.getFileName() + "," + this.getFileSize() + "," + this.getFileDOB() + "," + type + "," + this.getFileAddress() + "$";
			else
				str += this.getLinkName() + "," + this.getFileSize() + "," + this.getFileDOB() + "," + type + "," + this.getFileName() + "," + this.getFileAddress() + "$";
			return str;
		}
		public DateTime FileDOBValue()
		{
			DateTime dateTimeVal = new System.IO.FileInfo(this.getFileName()).CreationTime;
			return dateTimeVal;
		}

	}

	class FileInfoComparer : IComparer
	{

		int IComparer.Compare(Object x, Object y)
		{
			FileInfo f1 = x as FileInfo;
			FileInfo f2 = y as FileInfo;
			return (int)(f1.getFileAddress() - f2.getFileAddress());
		}
	}

	class FileComparerSize : IComparer
	{

		int IComparer.Compare(Object x, Object y)
		{
			FileInfo f1 = x as FileInfo;
			FileInfo f2 = y as FileInfo;
			return (int)(Convert.ToInt32(f1.getFileSize()) - Convert.ToInt32(f2.getFileSize()));
		}
	}

	class FileComparerDate : IComparer
	{

		int IComparer.Compare(Object x, Object y)
		{
			FileInfo f1 = x as FileInfo;
			FileInfo f2 = y as FileInfo;
			return DateTime.Compare(f1.FileDOBValue(), f2.FileDOBValue());
		}
	}

	class FileComparerAB : IComparer
	{

		int IComparer.Compare(Object x, Object y)
		{
			FileInfo f1 = x as FileInfo;
			FileInfo f2 = y as FileInfo;
			string n1;
			string n2;
			if (f1.isLink)
			{
				n1 = f1.getLinkName();
			}
			else
			{
				n1 = f1.getFileName();
			}
			if (f2.isLink)
			{
				n2 = f2.getLinkName();
			}
			else
			{
				n2 = f2.getFileName();
			}
			return String.Compare(n1, n2);


		}
	}

}
