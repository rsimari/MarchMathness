using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;

/*
	1: make a file named teams.txt
	2: fill the file with the format (Team Name) \n (Team ID number from espn.com)
   3. fill conferenceTop25.txt with the number of each conference in top25
	3: Run program
  notes: if there is no score or name on the game that means the team is not D1
*/

namespace Basketball {
   
   class Team {
      private string name;
      private string conference;
      public double nonConfMargin = 0; // non conf margin
      public double confMargin = 0;
      private string id;
      //public double conferenceCoeff;
      private string URL;
      private string STATS_URL; // http://espn.go.com/mens-college-basketball/team/stats/_/id/ + y + /
      public int [] stats = new int [11]; // look at html for order of stats
      public double winPer;
      public double nonConfWinPer;
      public int wins = 0; // total wins
      public int losses = 0; // total losses
      public int topWins = 0; // wins vs top 25
      public int topLosses = 0; // losses vs top 25
      public int nonConfWins = 0; // non conference wins
      public int nonConfLosses = 0; // non conference losses
      public int confWins = 0; // conference wins
      public int confLosses = 0; // conference losses
    	public int[,] games = new int[40,4]; // games[score1, score2, W/L, Rank of Opp]
      public string[] schedule = new string[40]; // filled with teams from their schedule
      public string[] confSchedule = new string[40]; // filled with conferences they played
      public int[] exp = new int[4]; // # of freshman, sophomores, juniors, seniors

      public string Name {
        get { return name; }
        set { name = value; }
      }
      public void setURL(string y) {
         URL = "http://espn.go.com/mens-college-basketball/team/schedule/_/id/" + y + "/";
         id = y;
      }
      public void setSTATS(string y) {
         STATS_URL = "http://espn.go.com/mens-college-basketball/team/stats/_/id/" + y + "/";
      }
      public int Wins { 
         get { return wins; }
      }
      public int Losses {
         get { return losses; }
      }
      public string Conference {
         get { return conference; }
         set { conference = value; }
      }
      public int [] Stats {
         get { return stats; }
         set { stats = value; }
      }
      public void getNonConf() {
         int i; 
         nonConfWins = 0;
         for (i = 1; i < wins+losses; i++)
            if (conference != confSchedule[i]) { // counts only wins and against non conf
               if (games[i,2] == 1) {
                  nonConfWins++; // a non conference win
                  nonConfMargin = nonConfMargin + (games[i,0]-games[i,1]); // also gets the total margin for non conf games
               } else {
                  nonConfLosses++; // gets totals of all kinds of wins
               }
            } else if (games[i,2] == 1) {
               confMargin = confMargin + (games[i,0]-games[i,1]);
               confWins++;
            } else {
               confLosses++;
            }
      }
      public int getConfWins() { return (wins - nonConfWins); }
      public List<string> printData() {
         char status;
         List<string> output = new List<string>();
       	for (int i = 1; i < wins + losses; i++) { // doesnt include the first game
       		if (games[i,2] == 1)
               status = 'W';
            else status = 'L';
            output.Add(status + "\t" + games[i,0].ToString() + " - " + games[i,1].ToString() + "  #" + games[i,3].ToString() + " " + schedule[i]);  
         }
         return output;
      }
      public void setData() {

         WebClient client = new WebClient(); 
         client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.0.3705;)");
     	   // downloads html as a string
         string file = client.DownloadString(URL);
         
     	   // gets scores of all the games
         getScores(file);
         // gets name of opponent & gets the rank of the opponents for each game
         getOpp(file);
         // gets number of each class (grade) on each team
         setExp();

         file = client.DownloadString(STATS_URL);
         string delim = "<td>Totals</td>";
         string [] url = new string[80];
         url = Regex.Split(file, delim);

         int i = 0;
         int n = 10;
         string u = url[1]; 
         while (n < u.Length && i < 11) {
            int x;
            if (int.TryParse(u.Substring(n,3), out x)) { // this gets the stats for the teams
               stats[i++] = x;
               n+=3;
            } else {
               if (int.TryParse(u.Substring(n,2), out x)) {
                  stats[i++] = x;
                  n++;
               } else {
                  if (int.TryParse(u.Substring(n,1), out x)) {
                     stats[i++] = x;
                  }
               }
            }
            n++;
         } 
      }
      public void getOpp(string file) {

         int i = 0;
         int t, size;
         string delim = "<li class=\"team-name\">";
         string [] ranks = Regex.Split(file, delim);

         // gets the rank of the team
         foreach (string y in ranks) {
            t = 0; size = 1;
            if (y[0] == '#') {
               if (y[2] != ' ') {
                  games[i,3] = (y[1] - '0') * 10 + (y[2] - '0');
               } else {
                  games[i,3] = y[1] - '0';
               }
               // check for top 25 win
               if (games[i,2] == 1)
                  topWins++;
               else if (games[i,2] == 0) 
                  topLosses++;
            } 
             
             // for the name of the team
            while (y[t] != '>') 
               t++;
            while (y[size + t] != '<')
               size++; 
            schedule[i++] = y.Substring(t+1, size-1);
         }
      }
      public void getScores(string file) {
         /* finds the scores of the teams */
         int i = 0;

         // to divide the html up into smaller strings
         string delim = "<li class=\"game-status "; 
         string [] scores = Regex.Split(file, delim);

         foreach (string x in scores) {  // a = 98, b = 101;  --> xx-yy, xxx-yy, xxx-yyy <--
            // these are the locations of the scores in the html.  a  b   a  b    a  b
            int score1, score2;
            int a = 98;
            int b = 101;
            if (x[0] == 'w') {
               wins++;
               games[i,2] = 1;
               // these are the scores from the html
               if (x[a+2] == '-') {
                  score1 = (x[a] - '0') * 10 + (x[a+1] - '0'); // <xx-yy>
                  score2 = (x[b] - '0') * 10 + (x[b+1] - '0');
               } else if (x[a+3] == '-') {
                  score1 = (x[a] - '0') * 100 + (x[a+1] - '0') * 10 + (x[a+2] - '0'); // <xxx-yy>
                  if (Char.IsNumber(x[a+6])) {
                     score2 = (x[b+1] - '0') * 100 + (x[b+2] - '0') * 10 + (x[b+3] - '0'); // <xxx-yyy>
                  } else score2 = (x[b+1] - '0') * 10 + (x[b+2] - '0');
               } else { score1 = 0; score2 = 0; }
            } else {
               losses++; 
               games[i,2] = 0;
               if (x[a+1] == '-') {
                  score1 = (x[a-1] - '0') * 10 + (x[a] - '0'); 
                  score2 = (x[b-1] - '0') * 10 + (x[b] - '0');
               } else if (x[a+2] == '-') {
                  score1 = (x[a-1] - '0') * 100 + (x[a] - '0') * 10 + (x[a+1] - '0'); 
                  if (Char.IsNumber(x[a+7])) {
                     score2 = (x[b] - '0') * 100 + (x[b+1] - '0') * 10 + (x[b+2] - '0');
                  } else score2 = (x[b] - '0') * 10 + (x[b+1] - '0');
               } else { score1 = 0; score2 = 0; } // because the compiler is dumb
            }
            // puts scores into game array;
            games[i,0] = score1;
            games[i,1] = score2;
            i++;
         }
      }
      public void setExp() {
         ProcessStartInfo startInfo = new ProcessStartInfo() {
            FileName = "/Users/bobsim21/Desktop/MarchMathness/C#/experience.sh",
            Arguments = id, // runs the script to find the number in each class for the team (exp)
         };
         Process proc = new Process() {
            StartInfo = startInfo,
         };
         proc.Start();
         StreamReader sr = File.OpenText("experience.txt");
         int i,x;
         for (i = 0; i < 4; i++) {
            if (int.TryParse(sr.ReadLine(), out x)) // this works
               exp[i] = x;
         }
         sr.Close(); // closes experience.txt
      }
      /*
      public void getWinPer() {
         return (wins * 1.0 / ((wins + losses) * 1.0 ));
      }
      public void getNonConfPer() {
         return (nonConfWins * 1.0 / ((nonConfWins + nonConfLosses) * 1.0));
      }
      */
   }

   class Conference {
      public Dictionary<string, string> conferences = new Dictionary<string, string>();

      public void setData() {
         StreamReader sr = File.OpenText("conference.txt"); // conferences followed by their teams
         string value = "", key = "";
         while (true) { // reads the conferences
            value = sr.ReadLine();
            if (value == null) // EOF
               break;
            while (true) {
               key = sr.ReadLine(); // reads the teams
               if (key == "" || key == null) // read until empty line
                  break;
               conferences.Add(key, value); // team, conference
            }   
         }
         // closes the conference.txt file
         sr.Close();
      }

      public string getConf(string team) {
         if (conferences.ContainsKey(team)) 
            return conferences[team];
         else return "null";
      }

      public void listAll() {
         // prints all values and keys
         foreach (KeyValuePair<string, string> myItem in conferences) 
            Console.WriteLine(myItem.Key + " : " + myItem.Value);
      }
   }

   class Execute {
      const int NUMBER_OF_TEAMS = 351; // NUMBER OF TEAMS HERE!
		// entry point
      static void Main(string[] args) {
         Conference ConferenceList = new Conference(); // list of teams -> conferences
         ConferenceList.setData();

         StreamReader sr = File.OpenText("teams.txt");
         List<string> output = new List<string>();
         List<string> temp = new List<string>();
         List<string> temp2 = new List<string>();
   		// array of teams in bracket
         int i = 0;
         string input;
         var teamArray = new Team[352];

         while(true) {
            teamArray[i] = new Team();
       	    // gets the name of the team, url of data and fetches data
       	   teamArray[i].Name = sr.ReadLine(); // team name
            input = sr.ReadLine(); // team id
            if (input == null)
               break;
       	   teamArray[i].setURL(input); // url for schedule
            teamArray[i].setSTATS(input); // url for stats
            teamArray[i].setData();
            // puts all the conferences in the data for each team
            teamArray[i].Conference = ConferenceList.getConf(teamArray[i].Name);
            char status;
            //List<string> output = new List<string>();
            
            //if (games[i,2] == 1) 
            //   output.Add((games[i,0]-games[i,1]).ToString()); // win margin
            //else output.Add((games[i,1]-games[i,0]).ToString()); // loss margin
            //else output.Add("0");
            
            // puts the conferences of the teams they played into an array
            for (int j = 1; j < teamArray[i].Wins + teamArray[i].Losses; j++) 
               teamArray[i].confSchedule[j] = ConferenceList.getConf(teamArray[i].schedule[j]);
            teamArray[i].getNonConf();

            string margin;
            output.Add(teamArray[i].Name);
            output.Add("|");
            //output.Add("---------------\n");
            for (int j = 1; j < teamArray[i].Wins + teamArray[i].Losses; j++) { // doesnt include the first game
               if (teamArray[i].games[j,2] == 1) {
                  status = 'W';
                  margin = (teamArray[i].games[j,0]-teamArray[i].games[j,1]).ToString();
               } else { 
                  status = 'L'; 
                  margin = "0";
               }
               output.Add((27 - teamArray[i].games[j,3]) + "|" + ConferenceList.getConf(teamArray[i].schedule[j]) + "|" + margin + "|" + "|" + "|");
               //output.Add(status + "\t" + margin + " #" + (27 - teamArray[i].games[j,3]) + "|" + ConferenceList.getConf(teamArray[i].schedule[j]) + "|" + teamArray[i].schedule[j]);
            }  

            // adds all output to be printed
            
            
            /*
            foreach (string x in temp) 
               output.Add(x);
            foreach (string y in temp2)
               output.Add(y);
            */
            //output.Add("\n");
            //Console.WriteLine(teamArray[.Length);
            
            //for (int y = 0; y < 11; y++) 
             //  output.Add((teamArray[i].Stats[y]).ToString());
            //for (y = 0; y < 40; y++)
            //   output.Add(
            output.Add("\n");
            //output.Add("===============\n");
            //Console.WriteLine("Non Conf Wins: " + teamArray[i].nonConfWins); // prints non conference wins

            i++;
            //Console.WriteLine(i);
         }

         // closes the teams.txt file
         sr.Close(); 
/*
         Dictionary<string, double> coeff = new Dictionary<string, double>(); // dictionary with all the conf coefficients
         Dictionary<string, double> winPercentageA = new Dictionary<string, double>(); // total win %
         Dictionary<string, double> winPercentageB = new Dictionary<string, double>(); // nonConf win %
*/
         // I need to total the margin for all the teams in a conference and then average them 
         // sum up all nonConfWins, nonConfLosses, wins, losses for each conference

         // for each key if they have the same value sum the teams stats stuff and count the teams
         // use another dictionary and either .Add or += to the value for the conferences
/*
         //log10 of (Avg. Total Pts Margin)^Teams in Top 25*(10^Avg. vs AP,USA)*(10^Interconf W%)  
                        ^^ coeff                               ^^ winPercentageA    ^^ winPercentageB  
                        if keys are the same multiply them together and then take Log10
         double a, b;    
         // fix the calculations
         for (i = 0; i < NUMBER_OF_TEAMS; i++) {
            if (!coeff.ContainsKey(teamArray[i].Conference)) {
               a = Math.Pow(10, (teamArray[i].wins / (teamArray[i].wins + teamArray[i].losses))); // win %
               b = Math.Pow(10, (teamArray[i].nonConfWins / (teamArray[i].nonConfWins + teamArray[i].nonConfLosses)));
               coeff.Add(teamArray[i].Conference, a * b); // create a key, value
            } else if (teamArray[i].Conference != null) {
               a = Math.Pow(10, (teamArray[i].wins / (teamArray[i].wins + teamArray[i].losses))); // win %
               b = Math.Pow(10, (teamArray[i].nonConfWins / (teamArray[i].nonConfWins + teamArray[i].nonConfLosses)));
               coeff[teamArray[i].Conference] += (a * b); // add to key, value
            }
         }
         // ^(teams in top 25)
         sr = File.OpenText("conferenceTop25.txt");
         string team;
         double number;
         for (i = 0; i < 32; i++) {
            team = sr.ReadLine(); // conference name
            number = Int32.Parse(sr.ReadLine()) * 1.0; // # of top 25 teams in conference
            coeff[team] = (Math.Pow(coeff[team], number)); // maths
         }
         // THIS WILL GET THE TOTAL WIN PERCENTAGES IF I CHANGE IT A LITTLE (add counter to divide it up)
         double wins = 0, winsB = 0, total = 0, totalB = 0, count = 0;
         foreach (var k in coeff.Keys.ToList()) {
            for (i = 0; i < NUMBER_OF_TEAMS; i++) { // goes through all the teams
               if (teamArray[i].Conference == k) {
                  wins += teamArray[i].topWins;
                  total += (teamArray[i].topWins + teamArray[i].topLosses);
                  winsB += teamArray[i].nonConfWins;
                  totalB += (teamArray[i].nonConfWins + teamArray[i].nonConfLosses);  
                  count++;
               }
            }
            Console.Write(coeff[k] + " --> " + wins + " " + winsB); // coeff[k] == total nonconference winning margin 
            if (totalB != 0 && total != 0) { // just to make sure 
               coeff[k] = Math.Log(coeff[k] * (wins/total * winsB/totalB), 10); // maths
            }
            wins = 0; winsB = 0; total = 0; totalB = 0; // reset
            Console.Write(k + " : ");
            Console.WriteLine(coeff[k]);
         }
*/
        
         // writes to out.txt
         StreamWriter writer = new StreamWriter("out2.txt");
         foreach (string x in output) {
            writer.Write(x);
         } 
      }
   }
}