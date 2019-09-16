<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">

<head>
	<title>Johns Hopkins Turbulence Databases (JHTDB)</title>

	<link href="../../bootstrap-3.2.0-dist/css/bootstrap.min.css" rel="stylesheet" type="text/css" />
	<script src="https://ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js"></script>
	<script src="../../bootstrap-3.2.0-dist/js/bootstrap.min.js"></script>
	<link href="../../turbulence.css" rel="stylesheet" type="text/css" />
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
				<!--#include file="../../navbar.htm" -->
				<div id="leftcolumn">
					<div id="custom" style="margin-top: 120px">
						<p>
							<img src="../../images/fig4-1.jpg" width="205" />
						</p>
					</div>

				</div>
				<div id="centercolumn">

					<h2 align="center"><span class="style22"><font size="+2">Using JHTDB with Python</font></span></h2>

					<h3>Download</h3>
					<h4><strong>Python code:  </strong>directly from <a href="https://github.com/idies/pyJHTDB/archive/master.zip">here</a> or <a href="https://github.com/idies/pyJHTDB">https://github.com/idies/pyJHTDB</a></h4>
					<p>
						This downloads a directory with sample IPython Notebook code in the <tt>examples</tt> folder that illustrate the basic functionality of the interface. Choose one of the following to use the pyJHTDB package.
						<br />
						Please see the <tt>README</tt> file for more information.
					</p>

					<h3>Use through SciServer (RECOMMENDED)</h3>
					The SciServer is a cloud-based data-driven cluster, of The Institute for Data Intensive Engineering and Science (IDIES) at Johns Hopkins University. Users get the advantages of more reliable and faster data access since the SciServer is directly connected to JHTDB through a 10 Gigabit ethernet connection. SciServer provides docker containers with the pyJHTDB library pre-installed.<br />
					<br />
					To use pyJHTDB through Sciserver:
					<p class="code">
						Login to SciServer <a href="http://www.sciserver.org" starget="_blank">http://www.sciserver.org</a> (may need to create a new account first).<br />
						Click on <b>Compute</b> and then <b>Create container</b> (You could also run jobs in batch mode, by selecting <b>Compute Jobs</b>).<br />
						Type in <b>Container name</b>, select <b>JH Turbulence DB</b> in <b>Compute Image</b> and then click on <b>Create</b>.<br />
						Click on the container you just created, then you could start using pyJHTDB with Python or IPython Notebook.<br />
					</p>
					Examples of using pyJHTDB could be found at <a href="https://github.com/idies/pyJHTDB">https://github.com/idies/pyJHTDB</a>.
					Please go to <a href="http://www.sciserver.org">http://www.sciserver.org</a> for more information on SciServer as well as the help on Sciserver.
					<h3>Use on local computers</h3>

					<h4>Installing pypi version</h4>
					If you have pip, you can simply do this:
					<p class="code">
						pip install pyJHTDB
					</p>
					If you're running unix (i.e. some MacOS or GNU/Linux variant), you will probably need to have a sudo in front of the pip command. If you don't have pip on your system, it is quite easy to get it following the instructions at <a href="http://pip.readthedocs.org/en/latest/installing.html">http://pip.readthedocs.org/en/latest/installing.html</a>.

					<h4>Installing from source</h4>
					In terminal:
					<p class="code">
						cd /path/to/your/folder/
						<br />
						git clone https://github.com/idies/pyJHTDB.git
						<br />
						cd pyJHTDB<br />
						python update_turblib.py<br />
						pip install --upgrade ./<br />
					</p>
					Note that doing this should update pyJHTDB and all the required packages, including numpy, scipy, sympy, h5py and matplotlib.

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
