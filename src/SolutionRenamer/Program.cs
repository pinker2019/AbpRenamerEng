using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SolutionRenamer
{
	static class Program
    {
        static void Main()
        {

	        Console.Title = "SolutionRenamer";

            //var fileExtensions = configuration["FileExtension"];
            var fileExtensions = ".cs,.cshtml,.js,.ts,.csproj,.sln,.xml,.xaml,.config";

            string[] filter = fileExtensions.Split(',');

			Console.WriteLine();
			Console.WriteLine("Input your company name(default:MyCompanyName):");
	        var oldCompanyName = Console.ReadLine();
            if (string.IsNullOrEmpty(oldCompanyName))
            {
                oldCompanyName = "MyCompanyName";
            }            

            Console.WriteLine("Input your peoject name(default:AbpZeroTemplate):");
	        var oldPeojectName = Console.ReadLine();
	        if (string.IsNullOrEmpty(oldPeojectName))
	        {
		        oldPeojectName = "AbpZeroTemplate";
	        }


			Console.WriteLine("Input your new company name(Any):");
	        var newCompanyName = Console.ReadLine();
            if (string.IsNullOrEmpty(newCompanyName))
            {
                oldCompanyName= "MyCompanyName.";
            }

            Console.WriteLine("Input your new peoject name(Required):");
	        var newPeojectName = Console.ReadLine();
	        if (string.IsNullOrEmpty(newPeojectName))
	        {
		        newPeojectName = "SHML";
	        }


			Console.WriteLine("Input folder(D:\aspnet-zero-core-6.2.0):");
	        var rootDir = Console.ReadLine();

			Stopwatch sp=new Stopwatch();

	        long spdir, spfile;

			sp.Start();
			RenameAllDir(rootDir,oldCompanyName, oldPeojectName,newCompanyName,newPeojectName);
			sp.Stop();
	        spdir = sp.ElapsedMilliseconds;
			Console.WriteLine("Directory rename complete! spend:" + sp.ElapsedMilliseconds);
			
			sp.Reset();
			sp.Start();
	        RenameAllFileNameAndContent(rootDir, oldCompanyName, oldPeojectName, newCompanyName, newPeojectName, filter);
	        sp.Stop();
	        spfile = sp.ElapsedMilliseconds;
			Console.WriteLine("Filename and content rename complete! spend:" + sp.ElapsedMilliseconds);

			Console.WriteLine("");
			Console.WriteLine("=====================================Report=====================================");
			Console.WriteLine($"Processing spend time,directories:{spdir},files:{spfile}");
			Console.ReadKey();
        }


        #region Recursively rename all directories

        /// <summary>
        /// Recursively rename all directories
        /// </summary>
        static void RenameAllDir(string rootDir,string oldCompanyName,string oldPeojectName,string newCompanyName,string newProjectName)
	    {
		    string[] allDir = Directory.GetDirectories(rootDir);

		    foreach (var item in allDir)
		    {
			    RenameAllDir(item, oldCompanyName,oldPeojectName,newCompanyName,newProjectName);

			    DirectoryInfo dinfo = new DirectoryInfo(item);
			    if (dinfo.Name.Contains(oldCompanyName)||dinfo.Name.Contains(oldPeojectName))
			    {
				    var newName = dinfo.Name;

					if (!string.IsNullOrEmpty(oldCompanyName))
					{
						newName = newName.Replace(oldCompanyName, newCompanyName);
					}
				    newName = newName.Replace(oldPeojectName,newProjectName);

			        if (dinfo.Parent != null)
			        {
			            var newPath = Path.Combine(dinfo.Parent.FullName, newName);

			            if (dinfo.FullName != newPath)
			            {
			                Console.WriteLine(dinfo.FullName);
			                Console.WriteLine("->");
			                Console.WriteLine(newPath);
			                Console.WriteLine("-------------------------------------------------------------");
			                dinfo.MoveTo(newPath);
			            }
			        }
			    }
		    }
	    }

        #endregion

        #region Recursively rename all file names and file contents

        /// <summary>
        /// 递归重命名所有文件名和文件内容
        /// </summary>
        static void RenameAllFileNameAndContent(string rootDir, string oldCompanyName, string oldPeojectName, string newCompanyName, string newProjectName, string[] filter)
	    {
	        //Get all files with the specified file extension in the current directory
            List<FileInfo> files =new DirectoryInfo(rootDir).GetFiles().Where(m=> filter.Any(f => f == m.Extension)).ToList();

	        //Rename current directory file and file content
            foreach (var item in files)
		    {

			    var text = File.ReadAllText(item.FullName, Encoding.UTF8);
			    if (!string.IsNullOrEmpty(oldCompanyName))
			    {
					text = text.Replace(oldCompanyName, newCompanyName);
				}
				
			    text = text.Replace(oldPeojectName, newProjectName);
			    if (item.Name.Contains(oldCompanyName)|| item.Name.Contains(oldPeojectName))
			    {
					var newName= item.Name;

				    if (!string.IsNullOrEmpty(oldCompanyName))
				    {
					    newName = newName.Replace(oldCompanyName, newCompanyName);

					}
				    newName = newName.Replace(oldPeojectName, newProjectName);
					var newFullName = Path.Combine(item.DirectoryName, newName);
				    File.WriteAllText(newFullName, text, Encoding.UTF8);
				    if (newFullName != item.FullName)
				    {
						File.Delete(item.FullName);
					}
				}
			    else
			    {
				    File.WriteAllText(item.FullName,text,Encoding.UTF8);
			    }
			    Console.WriteLine(item.Name+" process complete!");

		    }

	        //Get subdirectory
            string[] dirs = Directory.GetDirectories(rootDir);
		    foreach (var dir in dirs)
		    {
				RenameAllFileNameAndContent(dir, oldCompanyName, oldPeojectName, newCompanyName, newProjectName, filter);
			}
	    }

	    #endregion
	}

}
