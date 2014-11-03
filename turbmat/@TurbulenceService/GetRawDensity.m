function GetRawDensityResult = GetRawDensity(obj,authToken,dataset,time,X,Y,Z,Xwidth,Ywidth,Zwidth)
%GetRawDensity(obj,authToken,dataset,time,X,Y,Z,Xwidth,Ywidth,Zwidth)
%
%   Get a cube of the raw Density data with the given width cornered at the specified coordinates for the given time.
%   
%     Input:
%       authToken = (string)
%       dataset = (string)
%       time = (float)
%       X = (int)
%       Y = (int)
%       Z = (int)
%       Xwidth = (int)
%       Ywidth = (int)
%       Zwidth = (int)
%   
%     Output:
%       GetRawDensityResult = (base64Binary)

% Build up the argument lists.
values = { ...
   authToken, ...
   dataset, ...
   time, ...
   X, ...
   Y, ...
   Z, ...
   Xwidth, ...
   Ywidth, ...
   Zwidth, ...
   };
names = { ...
   'authToken', ...
   'dataset', ...
   'time', ...
   'X', ...
   'Y', ...
   'Z', ...
   'Xwidth', ...
   'Ywidth', ...
   'Zwidth', ...
   };
types = { ...
   '{http://www.w3.org/2001/XMLSchema}string', ...
   '{http://www.w3.org/2001/XMLSchema}string', ...
   '{http://www.w3.org/2001/XMLSchema}float', ...
   '{http://www.w3.org/2001/XMLSchema}int', ...
   '{http://www.w3.org/2001/XMLSchema}int', ...
   '{http://www.w3.org/2001/XMLSchema}int', ...
   '{http://www.w3.org/2001/XMLSchema}int', ...
   '{http://www.w3.org/2001/XMLSchema}int', ...
   '{http://www.w3.org/2001/XMLSchema}int', ...
   };

% Create the message, make the call, and convert the response into a variable.
soapMessage = createSoapMessage( ...
    'http://turbulence.pha.jhu.edu/', ...
    'GetRawDensity', ...
    values,names,types,'document');
response = callSoapService( ...
    obj.endpoint, ...
    'http://turbulence.pha.jhu.edu/GetRawDensity', ...
    soapMessage);
GetRawDensityResult = parseSoapResponse(response);
