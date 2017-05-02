using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

namespace TurbulenceService
{
    /// <summary>
    /// Summary description for DataInfo
    /// </summary>
    public class DataInfo
    {
        public enum DataSets : int
        {
            mhd1024 = 3,
            isotropic1024coarse = 4,
            isotropic1024fine = 5,
            channel = 6,
            mixing = 7,
            rmhd = 8,
            isotropic4096 = 10
        }

        // TODO: This needs to be refactored. We probably don't want to keep track of
        // the table names here, but only the datasets. One issue is that both the fine
        // and the coarse isotropic datasets are stored in the same table. The stored 
        // procedure should take only the dataset name as parameter and we can include
        // and extra "field" parameter to distinguish between velocity, pressure, magnetic, etc.
        public enum TableNames : int
        {
            //isotropic1024coarse, //Original isotropic turb. DB data table is "isotropic1024data"
            //                     //this is the name of the dataset, which is required by the stored procedure
            //isotropic1024fine,   //The name of the dataset is required by the stored procedure
            velocity08, //MHD DB Velocity table
            pressure08, //MHD DB Pressure table
            magnetic08, //MHD DB Magnetic table
            potential08, //MHD DB Potential table
            vel, //Isotropic Turb. DB Velocity table, Channel DB Velocity table
            pr, //Isotropic Turb. DB Pressure table, Channel DB Pressure table
            isotropic1024fine_vel,
            isotropic1024fine_pr,
            density, // Mixing DB Density table
            mag // Magnetic field for RMHD
        }

        public static int getNumberComponents(TableNames tableName)
        {
            switch (tableName)
            {
                case TableNames.density:
                    return 1;
                case TableNames.isotropic1024fine_pr:
                    return 1;
                case TableNames.isotropic1024fine_vel:
                    return 3;
                case TableNames.magnetic08:
                    return 3;
                case TableNames.mag:
                    return 2;
                case TableNames.potential08:
                    return 3;
                case TableNames.pr:
                    return 1;
                case TableNames.pressure08:
                    return 1;
                case TableNames.vel:                    
                    return 3;
                case TableNames.velocity08:
                    return 3;
                default:
                    throw new Exception("Invalid field specified!");
            }
        }

        public static TableNames getTableName(DataSets dataSet, string field)
        {
            switch (dataSet)
            {
                case DataSets.isotropic1024fine:
                    if (field.Equals("u") || field.Contains("vel") || field.Contains("Vel") || field.Contains("vorticity") || field.Equals("q") || field.Equals("Q"))
                        return TableNames.isotropic1024fine_vel;
                    else if (field.Equals("p") || field.Contains("pr") || field.Contains("Pr"))
                        return TableNames.isotropic1024fine_pr;
                    else
                        throw new Exception("Invalid field specified!");
                case DataSets.isotropic1024coarse:
                    if (field.Equals("u") || field.Contains("vel") || field.Contains("Vel") || field.Contains("vorticity") || field.Equals("q") || field.Equals("Q"))
                        return TableNames.vel;
                    else if (field.Equals("p") || field.Contains("pr") || field.Contains("Pr"))
                        return TableNames.pr;
                    else
                        throw new Exception("Invalid field specified!");
                case DataSets.channel:
                    if (field.Equals("u") || field.Contains("vel") || field.Contains("Vel") || field.Contains("vorticity") || field.Equals("q") || field.Equals("Q"))
                        return TableNames.vel;
                    else if (field.Equals("p") || field.Contains("pr") || field.Contains("Pr"))
                        return TableNames.pr;
                    else
                        throw new Exception("Invalid field specified!");
                case DataSets.mhd1024:
                    if (field.Equals("u") || field.Contains("vel") || field.Contains("Vel") || field.Contains("vorticity") || field.Equals("q") || field.Equals("Q"))
                        return TableNames.velocity08;
                    else if (field.Equals("b") || field.Contains("mag") || field.Contains("Mag"))
                        return TableNames.magnetic08;
                    else if (field.Equals("a") || field.Contains("vec") || field.Contains("pot") || field.Contains("Vec") || field.Contains("Pot"))
                        return TableNames.potential08;
                    else if (field.Equals("p") || field.Contains("pr") || field.Contains("Pr"))
                        return TableNames.pressure08;
                    else
                        throw new Exception("Invalid field specified!");
                //case DataSets.rmhd:
                //    if (field.Equals("u") || field.Contains("vel") || field.Contains("Vel") || field.Contains("vorticity") || field.Equals("q") || field.Equals("Q"))
                //        return TableNames.vel;
                //    else if (field.Equals("b") || field.Contains("mag") || field.Contains("Mag"))
                //        return TableNames.mag;
                //    else
                //        throw new Exception("Invalid field specified!");
                case DataSets.mixing:
                    if (field.Equals("u") || field.Contains("vel") || field.Contains("Vel") || field.Contains("vorticity") || field.Equals("q") || field.Equals("Q"))
                        return TableNames.vel;
                    else if (field.Equals("p") || field.Contains("pr") || field.Contains("Pr"))
                        return TableNames.pr;
                    else if (field.Equals("d") || field.Contains("density") || field.Contains("Density"))
                        return TableNames.density;
                    else
                        throw new Exception("Invalid field specified!");
                default:
                    throw new Exception("Invalid data set specified!");
            }
        }

        public static string GetCharFieldName(string field)
        {
            if (field.Equals("u") || field.Contains("vel") || field.Contains("Vel") || field.Contains("vorticity") || field.Equals("q") || field.Equals("Q"))
                return "u";
            else if (field.Equals("b") || field.Contains("mag") || field.Contains("Mag"))
                return "b";
            else if (field.Equals("a") || field.Contains("vec") || field.Contains("pot") || field.Contains("Vec") || field.Contains("Pot"))
                return "a";
            else if (field.Equals("p") || field.Contains("pr") || field.Contains("Pr"))
                return "p";
            else if (field.Equals("d") || field.Contains("density") || field.Contains("Density"))
                return "d";
            else
                throw new Exception("Invalid field specified!");
        }

        // {"external name", "internal name"}
        // The offsets are used in the log, so names should only be added to this list.
        private static string[,] sets = { {"isotropic1024", "isotropic1024coarse"},
                               {"isotropic1024coarse", "isotropic1024coarse"},
                               {"isotropic1024fine", "isotropic1024fine"},
                               {"mhd1024coarse", "mhd1024"},
                               {"rmhd", "rmhd"},
                               {"mhd1024", "mhd1024"},
                               {"channel", "channel"},
                               {"isotropic4096", "isotropic4096" },
                               {"mixing", "mixing"}};

        public static string findDataSet(string setname)
        {
            if (setname != null)
            {
                for (int i = 0; i < sets.GetLength(0); i++)
                {
                    if (sets[i, 0].Equals(setname.ToLower()))
                        return sets[i, 1];
                }
            }
            throw new Exception(String.Format("Invalid set name: {0}", setname));
        }

        // used to map dataset names to numbers stored in the log
        public static int findDataSetInt(string setname)
        {
            return (int)Enum.Parse(typeof(DataSets), setname);
        }

        /* Hard coded hack to verify time ranges.
         * TODO: Make this more general purpose in the future.
         */
        public static bool isTimeInRange(DataSets dataset, float time)
        {
            if (time < 0)
            {
                return false;
            }
            else if (dataset == DataSets.isotropic1024coarse && time > 10.056F)
            {
                return false;
            }
            else if (dataset == DataSets.isotropic1024fine && time > 0.0198F)
            {
                return false;
            }
            else if (dataset == DataSets.mhd1024 && time > 2.5601F)
            {
                return false;
            }
            else if (dataset == DataSets.rmhd && time > 2.5601F)
            {
                return false;
            }
            else if (dataset == DataSets.channel && time > 25.9935F)
            {
                return false;
            }
            else if (dataset == DataSets.mixing && time > 40.44F)
            {
                return false;
            }

            return true;
        }

        public static void verifyTimeInRange(DataSets dataset, float time)
        {
            if (!isTimeInRange(dataset, time))
            {
                throw new Exception(String.Format("Time {0} out of range for \"{1}\".", time, dataset));
            }
        }

        public static void verifyRawDataParameters(int X, int Y, int Z, int Xwidth, int Ywidth, int Zwidth)
        {
            if ((X % 8 != 0) || (Y % 8 != 0) || (Z % 8 != 0) || (Xwidth % 8 != 0) || (Zwidth % 8 != 0) || (Ywidth % 8 != 0))
            {
                throw new Exception(String.Format("One of X, Y, Z, Xwidth, Ywidth, Zwidth is not a multiple of 8!Currently " +
                    " getting raw data works only for positions and sizes that are a multiple of 8!"));
            }
        }

    }
}