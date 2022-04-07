using System;
using System.Windows.Forms;
using Salaros.Configuration;
using System.DirectoryServices;
using Pastel;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

namespace GetUserInfo
{
    public class Program
    {
        public static string infoToSearch = "!NA!";
        public static ConfigParser configs;
        
        [STAThread]
        public static void Main(){

            configs = Helpers.PrepareConfigs();
            infoToSearch = Clipboard.GetText();
            Helpers.PrintHeaders( infoToSearch );

            string query = Helpers.PrepareQueryWithInfo(infoToSearch);
            CustomUserInfo userFound = FindUserFromAD(query);
            userFound.PrintUserInfos();

            Helpers.SetClipboard(userFound);
            Console.ReadLine();
            Environment.Exit(0);
        }

        
        public static CustomUserInfo FindUserFromAD(string query){
            // KÖTÜ KOD sırası
            List<CustomUserInfo> allUserResults = new List<CustomUserInfo>();
            SearchResultCollection searchResult = new DirectorySearcher( new DirectoryEntry(), query).FindAll();
            string multVerbConf = configs.GetValue( "Strings", "multipleUserVerbose", "-1" );
            int multiUserVerbose = int.TryParse( multVerbConf, out _ ) ? int.Parse( multVerbConf ) : -1;
            string critAccFlag = configs.GetValue( "Strings", "critAccFlag", "-1" );

            foreach(SearchResult result in searchResult) { allUserResults.Add( result.GetUser() ); }

            if(allUserResults.Count == 0) { 
                Console.WriteLine( "\n\n\t\tCANNOT FOUND THE USER SPECIFIED with this : ".Pastel( Color.Red ) + infoToSearch );
                Console.ReadLine();
                Environment.Exit( 0 );
                return null;
            }
            else if(allUserResults.Count == 1) { return allUserResults.First(); }
            else if(allUserResults.Count == 2) {
                // this is because AD has 2 account for a user ( the other one is like an email account) maybe missconfigure?
                if( allUserResults.GroupBy( user => user.userPhone ).Count() == 1 ||
                    allUserResults[0].userSicil.Replace(critAccFlag, "") == allUserResults[1].userSicil.Replace( critAccFlag, "" ) ) { // there is duplicate users
                    allUserResults.RemoveAll( user => user.userSicil.Contains( '.' ) );
                    allUserResults.RemoveAll( user => user.userSicil.Contains( critAccFlag ) );
                }
                if( allUserResults.Count == 1) { return allUserResults.First(); }
            }

            Console.WriteLine( "\n\n\t\tFOUND MULTIPLE USERs with this : ".Pastel( Color.Red ) + infoToSearch + "\n\n" );
            allUserResults.ForEach( user => { user.PrintUserInfos( multiUserVerbose ); } );

            Console.ReadLine();
            Environment.Exit( 0 );
            return null;
        }
    }
}