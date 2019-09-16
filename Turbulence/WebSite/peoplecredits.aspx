<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">

<head>
	<title>Johns Hopkins Turbulence Databases (JHTDB)</title>

	<link href="bootstrap-3.2.0-dist/css/bootstrap.min.css" rel="stylesheet" type="text/css" />
	<script src="Scripts/jquery.min.js"></script>
	<script src="bootstrap-3.2.0-dist/js/bootstrap.min.js"></script>
	<link href="turbulence.css" rel="stylesheet" type="text/css" />
	<script type="text/javascript">
		function wait_message() {
			document.getElementById("message").innerHTML = "<b>Please wait, your request is being processed. Depending on the size of your request, this may take a moment</b>";
		}

	</script>
</head>
<body>
	<div id="pagewrapper">

		<div class="content">
			<div id="main">
				<div class="transparency"></div>
				<div id="title">
					<br />
					<p>Johns Hopkins Turbulence Databases</p>

				</div>
				<!--#include file="navbar.htm" -->
				<div id="leftcolumn">
					<p>
						<img src="images/fig4-1.jpg" width="205" /></p>
					<p><a href="images/fig2.jpg" target="_blank">
						<img src="images/fig2.jpg" width="205" /></a></p>
				</div>
				<div id="centercolumn">

					<p align="left" class="style21"><strong><font size="+2">People and credits:</font></strong></p>

					<p class="style23">Faculty:</p>
					<ul class="style24">
						<li><a href="https://randalburns.github.io/">Randal Burns</a> (Computer Science)</li>
						<li><a href="http://www.ams.jhu.edu/~eyink/">Gregory Eyink</a> (Applied Mathematics &amp; Statistics)</li>
						<li><a href="http://www.me.jhu.edu/meneveau/">Charles Meneveau</a> (Mechanical Engineering)</li>
						<li><a href="http://www.sdss.jhu.edu/~szalay/">Alex Szalay</a> (Physics and Astronomy)</li>
						<li><a href="https://engineering.jhu.edu/zaki/">Tamer Zaki</a> (Mechanical Engineering)</li>
						<li><a href="http://physics-astronomy.jhu.edu/directory/ethan-vishniac/">Ethan Vishniac</a> (Physics and Astronomy)</li>
					</ul>



					<p class="style23">Graduate students:</p>
					<ul class="style24">
						<li>Akshat Gupta (Applied Math and Statistics)</li>
						<li>Mengze Wang (Mechanical Engineering)</li>
						<li>Yue Hao (Mechanical Engineering)</li>
					</ul>

					<p class="style23">Postdocs and senior research associates:</p>
					<ul class="style24">


						<li>Zhao Wu (Mechanical Engineering)</li>
						<li>Gerard Lemson (Physics and Astronomy)</li>

					</ul>
					<p class="style23">External collaborators:</p>
					<ul class="style24">
						<li><a href="https://www.sustech.edu.cn/en/chenshiyi-profile.html">Shiyi Chen</a> (Beijing University, China)</li>
						<li><a href="https://sites.google.com/view/mklee">MyoungKyu Lee</a></li>
						<li>Nicholas Malaya</li>
						<li><a href="http://www.me.utexas.edu/faculty/faculty-directory/moser">Robert D. Moser</a></li>
						<li>Jesus Pulido</li>
						<li><a href="http://public.lanl.gov/livescu/">Daniel Livescu</a></li>
						<li><a href="http://www.me.gatech.edu/faculty/yeung">P.K. Yeung </a></li>
					</ul>

					<p class="style23">Visitors:</p>
					<ul class="style24">
						<li>Edo Frederix (Eindhoven Univ., visiting student summer 2011)</li>
						<li>Kai Buerger (Technical Univ. Muenchen, visiting student, summer 2011)</li>
						<li>José-Hugo Elsas (Universidade Federal do Rio de Janeiro, visiting PhD student 2016-2017)</li>
						<li>German Saltar (Univ. of Puerto Rico, visiting student summer 2019)</li>
					</ul>

					<p class="style23">Technical Staff:</p>
					<ul class="style24">
						<li>Victor Paul</li>
						<li>Jan vandenBerg</li>
						<li>Suzanne Werner</li>
					</ul>

					<p class="style23">Former group members:</p>
					<ul class="style24">
						<li><a href="http://cnls.lanl.gov/External/people/Hussein_Aluie.php">Hussein Aluie</a></li>
						<li><a href="https://physics-astronomy.jhu.edu/directory/tamas-budavari/">Tam&aacute;s Budav&aacute;ri </a>(Physics and Astronomy)</li>
						<li><a href="http://perso.ens-lyon.fr/laurent.chevillard/">Laurent Chevillard</a></li>
						<li>Ed Givelberg (Physics and Astronomy)</li>
						<li>Jason Graham</li>
						<li><a href="http://www.cs.jhu.edu/~kalin/Site/Welcome.html">Kalin Kanov</a> (Computer Science)</li>
						<li><a href="https://www.ds.mpg.de/3099125/Lalescu">Cristian Constantin Lalescu</a> (Applied Math and Statistics)</li>
						<li><a href="http://yi-li.staff.shef.ac.uk/">Yi Li</a></li>
						<li><a href="https://www.yikes.com/~eric/">Eric Perlman</a></li>
						<li>Minping Wan</li>
						<li><a href="http://www.cs.jhu.edu/~xwang/">Xiaodan Wang</a></li>
						<li><a href="http://idies.jhu.edu/affiliate/alainna-white/">Alainna White</a></li>
						<li><a href="https://www.ds.mpg.de/3099161/Wilczek">Michael Wilczek</a> (Mechanical Engineering)</li>
						<li><a href="http://en.coe.pku.edu.cn/faculty/facultybydept/mechanicandengineeringscience/891420.htm">Zuoli Xiao</a></li>
						<li>Yunke Yang</li>
						<li><a href="http://et2.engr.iupui.edu/main/people/detail.php?id=whyu">Huidan Yu</a></li>
						<li>Kung Yang</li>
						<li><a href="http://www.cs.jhu.edu/~stephenh/">Stephen Hamilton</a> (Computer Science)</li>
						<li><a href="https://profiles.stanford.edu/perry-johnson">Perry Johnson</a> (Mechanical Engineering)</li>
						<li><a href="https://sites.google.com/view/jinleecfd">Jin Lee</a> (Mechanical Engineering)</li>
						<li><a href="https://www.bennett.edu.in/faculties/dr-mohammad-danish/">Mohammad Danish</a> (Mechanical Engineering)</li>
						<li>Rohit Ravoori (Computer Science)</li>
					</ul>




					<p class="style22">&nbsp; </p>
					<div align="center"></div>
					<span class="style24">The Turbulence Database Group and the JHTDB project is funded through the 
      <a href="http://www.nsf.gov">National Science Foundation</a>.</span></td>
    
				</div>
				<div id="rightcolumn">
				</div>
			</div>
			<!-- Main -->

			<div id="bottom">
				Disclaimer: <em>While many efforts have
            been made to ensure that these data are accurate and reliable within
            the limits of the current state of the art, neither JHU nor any other
            party involved in creating, producing or delivering the website shall
            be liable for any damages arising out of users' access to, or use
            of, the website or web services. Users use the website and web services
            at their own risk. JHU does not warrant that the functional aspects
            of the website will be uninterrupted or error free, and may make
            changes to the site without notice. </em>
				<p align="center" class="style22 style32">
					<font face="Arial, Helvetica, sans-serif" color="#000033" size="-2">Last update: <%=System.IO.File.GetLastWriteTime(Server.MapPath(Request.Url.AbsolutePath)).ToString()%></font>
				</p>
			</div>
		</div>
		<!--close content.  Used for transparency -->
	</div>
	<!-- wrapper -->


</body>
</html>
