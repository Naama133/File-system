# file-system:
Basic file system interface, managed within a single file.

This file system save (for each file) the file-name, size, date & time of creation, and it's content. 

Additionally, the system supports link-files (shortcut to other file exists in the system).
Those links doesn't contain a copy of the data, only reference to it. 

Functionality:
1. Create file-system
2. Dir
3. Add a new file or link-file to the system
4. Remove file from the system
5. Rename file (and its links) stored within the system
6. Extract file from the system (save it to a given local directory).
7. Sort system files by: alphabetical order, creation date, or by size.
