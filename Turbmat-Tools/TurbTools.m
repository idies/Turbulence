%
% Turbmat-Tools - a Matlab library for querying, processing and visualizing
% data from the JHU Turbulence Database
%   
% TurbCache, part of Turbmat-Tools
%

%
% Written by:
% 
% Edo Frederix 
% The Johns Hopkins University / Eindhoven University of Technology 
% Department of Mechanical Engineering 
% edofrederix@jhu.edu, edofrederix@gmail.com
%
% Modified by:
%
% Jason Graham
% The Johns Hopkins University
% Department of Mechanical Engineering
% jgraha8@gmail.com
%

%
% This file is part of Turbmat-Tools.
% 
% Turbmat-Tools is free software: you can redistribute it and/or modify it
% under the terms of the GNU General Public License as published by the
% Free Software Foundation, either version 3 of the License, or (at your
% option) any later version.
% 
% Turbmat-Tools is distributed in the hope that it will be useful, but
% WITHOUT ANY WARRANTY; without even the implied warranty of
% MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General
% Public License for more details.
% 
% You should have received a copy of the GNU General Public License along
% with Turbmat-Tools.  If not, see <http://www.gnu.org/licenses/>.
%

classdef TurbTools < handle
    
    properties (Constant)
        % ---- Global constants ----
        TIME_OFFSET_MAX = 1024
        SPACING = 2.0*pi/1024
        TIME_SPACING = 0.002
        KOLMOGOROV_LENGTH = 0.00287
        KOLMOGOROV_TIME = 0.0446
        VISCOSITY = 0.000185
        DISSIPATION_RATE = 0.0928
        V_RMS = 0.681;
        MAX_POINTS_DIM = [4096 128 32] % max points per dimenision, for 1D, 2D and 3D      
        

        % ---- Turbulence database parameters (see documentation for definitions) ----
        NO_T_INT   = 'None'
        PCHIP_INT = 'PCHIP'
        NO_S_INT = 'None'
        LAG4_INT   = 'Lag4'
        LAG6_INT   = 'Lag6'
        LAG8_INT   = 'Lag8'
        FD4_DIFF_NO_INT = 'None_Fd4'
        FD6_DIFF_NO_INT = 'None_Fd6'
        FD8_DIFF_NO_INT = 'None_Fd8'
        FD4_DIFF_LAG4_INT  = 'Fd4Lag4'
        
        % ---- Interpolation for 3d isosurface plot
        ISOSURF_INTERP = '*cubic'
    end
    
    properties
        % ---- General settings with default values ----
        c_authkey = 'edu.jhu.pha.turbulence.testing-201104'
        c_dataset = 'isotropic1024coarse'
        c_spatialInt
        c_spatialDiff
        c_temporalInt
        i_maxQuiver = 32
        c_colormap = 'Jet'
        
        % ---- Cache variables ----
        RC
        c_cacheFile

    end
    
    methods
        %
        % ---- Initiate class ----
        %
        
        function PT = TurbTools(useCache)
            
            % Assign default parameter values
            PT.c_spatialInt = PT.NO_S_INT;
            PT.c_temporalInt = PT.NO_T_INT;
            PT.c_spatialDiff = PT.FD4_DIFF_NO_INT;
            
            % See if we have an authtoken.txt file
            fid = fopen('authtoken.txt');
            if fid > 0
                % fgets retains the newline character and breaks on Linux
                %token = fgets(fid);
                
                % fgetl disregards newline character and works on Linux
                %token = fgetl(fid);
                
                % Makes no assumption a/b newline character - just reads
                % string - probably more portable
                token = fscanf(fid,'%s');
                
                if ischar(token)
                    PT.c_authkey = token;
                end
                fclose(fid);
            end
            
	    % Check if Turbmat is already in path
            if ~exist('TurbulenceService', 'file')
                
                % Look for Turbmat in Turbmat-Tools and parent directories
                searchLevels=2;
                searchPath=fileparts(which('TurbTools'));
                if ispc
                    cdParent='\..';
                else
                    cdParent='/..';
                end
                
                for n=0:searchLevels
                    
                    if n > 0
                        searchPath=strcat(searchPath,cdParent);
                    end
                    
                    thisPath = searchPath;
                    
                    addpath(thisPath);
                    if( exist('TurbulenceService', 'file') )
                        set=1;
                        fprintf('Using Turbmat library from %s\n', thisPath);
                        break;
                    else
                        rmpath(thisPath);
                    end
                    
                    a = dir(thisPath);
                    
                    % Create cell array of child directories
                    b = cell(numel(a),1);
                    for i = 1:numel(a)
                        b(i) = {a(i).name};
                    end
                    % Sort to get sorted index
                    [~, sortIndx]=sort(b);
                    clear b;
                    
                    % Check child directories
                    set = 0;
                    for i = numel(a):-1:1
                        
                        % Extract index of descending order
                        j=sortIndx(i);
                        
                        if a(j).isdir && ...
                                ~isempty(regexpi(a(j).name, 'turbmat')) && ...
                                isempty(regexpi(a(j).name, 'turbmat-tools'))
                            
                            newPath=sprintf('%s/%s', thisPath, a(j).name);
                            
                            addpath(newPath);
                            if( exist('TurbulenceService', 'file') )
                                set=1;
                                fprintf('Using Turbmat library from %s\n', newPath);
                                break;
                            else
                                rmpath(newPath);
                            end
                            
                        end
                        
                    end
                    
                    if set
                        break;
                    end
                    
                end
                
                if ~set || ~exist('TurbulenceService', 'file')
                    error('Could not find Turbmat package. Make sure to include a copy of Turbmat in the Turbmat-Tools path.');
                end
            else
                % Extract path from TurbulenceService location
                turbservPath = fileparts(which('TurbulenceService'));
                realPath = regexp(turbservPath, '^(?<path>.*)[\/]@?TurbulenceService$', 'tokens');
                
                if numel(realPath)
                    fprintf('Using Turbmat library from %s\n', realPath{1}{1});
                else
                    fprintf('Using Turbmat library from %s\n', turbservPath);
                end
            end
            
            % Start TurbCache class
            if useCache
                PT.RC = TurbCache(PT);
            end            
        end
        
        %
        % ---- Validation functions ----
        %
        
        % Check a single string. The first argument to this function is the
        % char to be checked. The second argument is the class of variable
        % that is to be returned (int, float, char). The third optional
        % argument provides a regular expression to which the input is
        % compared. The fourth optional argument expects an array of two
        % values, to which the numerical value of the input is bounded. The
        % function will return the value in the requested data class, or
        % will throw an error. 
        function ret = checkChar(~, input, clss, regex, bound)
            
            if ~ischar(input)
                error('Invalid input class, expecting char');
            end
            
            if nargin >= 4 && ~isempty(regex)
               if isempty(regexp(input, regex, 'once'))
                   error('Input did not match with provided regular expression');
               end
            end
            
            % create return value for different classes
            if strcmp(clss, 'int')
                ret = round(str2double(input));
            end            
            if strcmp(clss, 'float')
                ret = str2double(input);
            end
            if strcmp(clss, 'char')
                ret = input;
            end
            
            % check range
            if isnumeric(ret) && nargin == 5
                if numel(bound) == 2
                    if ret < bound(1) || ret > bound(2)
                        error('Input is out of specified bounds');
                    end
                else
                    error('Provided boundaries are not correct, expecting column or row of two values');
                end
            end
            
        end
        
        % Loop through a cell of chars, and feed every char to the
        % checkChar function. Return an array of values.
        function ret = checkCellChars(PT, input, nitems, clss, regex, bound)
            
            if ~iscell(input)
                error('Invalid input class, expecting cell');
            end
            
            % check number of items
            n = numel(input);
            if numel(nitems) == 2
                if n < nitems(1) || n > nitems(2)
                    error('The provided cell contains not the correct number of items');
                end
            end
            
            % check single items
            ret = zeros(1,n);
            for i = 1:n
                if nargin == 4
                    ret(i) = PT.checkChar(input{i}, clss);
                elseif nargin == 5
                    ret(i) = PT.checkChar(input{i}, clss, regex);
                else
                    ret(i) = PT.checkChar(input{i}, clss, regex, bound);
                end
            end
            
        end
        
        %
        % ---- Input functions ----
        %
        
        % Ask input from the user with the inputdlg function. A single cell
        % result is converted to a char
        function result = askInput(~, cl_questions, cl_defaults)
            if length(cl_questions) == length(cl_defaults)
                temp = inputdlg(cl_questions, 'Input for TurbTools', 1, cl_defaults);
                
                if length(temp) > 1
                    result = temp;
                else
                    % Let's return a char
                    result = char(temp);
                end
            else
                error('The number of questions should equal the number of defaults');
            end
        end
        
        % Ask input from the user with the inputdlg function. A single cell
        % result is converted to a char
        function result = askYesno(~, c_question, c_default)
            result = questdlg(c_question, 'Input for TurbTools', 'Yes', 'No', c_default);
        end        
        
        % Ask the user to select an option, using the listdlg function
        function result = askOptions(~, cl_questions, cl_options)
            
            % return char, for consistency
            result = num2str(listdlg('PromptString', cl_questions{1}, ...
                             'SelectionMode', 'single', ...
                             'ListString', cl_options, ...
                             'Name', 'Input for TurbTools', ...
                             'ListSize', [300 100]));
            
        end
        
        %
        % ---- General functions ----
        %
        
        % Check if the provided class property is set. An undefined
        % property equals '[]' and is of the class 'double'
        function bool = isset(~, var)
            if isempty(var) && strcmp(class(var), 'double')
                bool = false;
            else
                bool = true;
            end
        end
        
        % Return an 's' if the input is larger than one. Useful for
        % postfixing an 's' to a word
        function result = plural(~, i)
            if i > 1
                result = 's';
            else
                result = '';
            end
        end
        
        % Factor increase rather than linear increase (as linspace)
        function result = factorspace(~, strt, nd, steps)
            fac = (nd/strt)^(1/(steps-1));
            result = zeros(1, steps);
            result(1) = strt;
            result(end) = nd;
            for i = 2:(steps-1);
                result(i) = result(i-1)*fac;
            end
        end
        
        % We have received the number of physical points we want to query.
        % This may be too much. In that case we need to ditribute the
        % number of max allowed points evenly over that direction. This
        % function will set m_nQueryPoints (the real number of points that
        % will be queried) and m_spacing, which contains the uniform spacing
        % between two adjacent points in one direction
        function [m_nQueryPoints m_spacing] = calculateQueryPoints(PT, m_nPoints)

            ndim = numel(m_nPoints);
            nmax = PT.MAX_POINTS_DIM(ndim);
            m_nQueryPoints = zeros(1, ndim);
            
            m_spacing = zeros(1, ndim);
            for i = 1:ndim
                if m_nPoints(i) > nmax
                    % Use nmax points
                    m_spacing(i) = PT.SPACING * (m_nPoints(i)-1) / (nmax-1);
                    m_nQueryPoints(i) = nmax;
                else
                    % Use nPoints
                    m_spacing(i) = PT.SPACING;
                    m_nQueryPoints(i) = m_nPoints(i);
                end
            end
            
            if ndim == 1
                fprintf('Querying %i physical grid points with %i query points\n', m_nPoints(1), m_nQueryPoints);
            elseif ndim == 2
                fprintf('Querying %ix%i physical grid points with %ix%i query points\n', m_nPoints(1:2), m_nQueryPoints);
            else
                fprintf('Querying %ix%ix%i physical grid points with %ix%ix%i query points\n', m_nPoints(1:3), m_nQueryPoints);
            end
        end
                
        %
        % ---- 2D Surface functions ----
        %
        
        % Set the two directions of a surface, depending on the
        % direction of the normal
        function chars = setDirections(~, c_surfDirection)
            if c_surfDirection == 'x'; chars(1) = 'z'; else chars(1) = 'x'; end
            if c_surfDirection == 'y'; chars(2) = 'z'; else chars(2) = 'y'; end
            chars(3) = c_surfDirection;
        end
        
        % This function creates a 3 x i_points matrix with the x-, y-, and
        % z-components of the points to be queried. By looking at
        % c_directions, we properly set the surface direction
        function m_points = fillRectangle(~, m_nQueryPoints, m_offsets, m_spacing, c_directions)
            
            lind1 = linspace(0, (m_nQueryPoints(1)-1)*m_spacing(1), m_nQueryPoints(1)) + m_offsets(1);
            lind2 = linspace(0, (m_nQueryPoints(2)-1)*m_spacing(2), m_nQueryPoints(2)) + m_offsets(2);
            
            m_points(strfind('xyz', c_directions(1)),:) = repmat(lind1, 1, m_nQueryPoints(2));
            m_points(strfind('xyz', c_directions(2)),:) = reshape(repmat(lind2, m_nQueryPoints(1), 1), 1, m_nQueryPoints(1)*m_nQueryPoints(2));
            m_points(strfind('xyz', c_directions(3)),:) = m_offsets(3);

        end       
        
        %
        % ---- 3D Volume functions ----
        %

        % This function creates a 3 x i_points matrix with on row 1 all
        % the direction1-coordinates, on row 2 all direction2-coordinates
        % are given, and on row 3 all direction3-coordinates
        function m_points = fillBlock(~, m_nQueryPoints, m_offsets, m_spacing)
            
            n = prod(m_nQueryPoints);
            m_points = zeros(3, n);
            for i = 1:3
                k = prod(m_nQueryPoints(1:(i-1)));
                l = prod(m_nQueryPoints((i+1):3));

                m_points(i, :) = reshape( ...
                                    repmat( ...
                                        linspace(0, (m_nQueryPoints(i)-1)*m_spacing(i), m_nQueryPoints(i))+m_offsets(i), ...
                                    k, l), ...
                                 1, n);  
            end     
        end
        
        %
        % ---- Turbulence database functions ----
        %
        
        % This function is a wrapper to the get*.m function files. It
        % checks if all required variables are set. Also, if caching is
        % enabled, it checks for present cache. Else it serves the request
        % directly from the turbulence database.
        function result = callDatabase(PT, method, i_points, m_points, f_time, useCache)
            
            if i_points > 4096 && ~isempty(regexp(PT.c_authkey, 'edu\.jhu\.pha\.turbulence\.testing', 'once'))

                cl_questions = {'You are querying more than 4096 points. Please use a different authentication token. Consult the README for more information.'};
                cl_defaults = {PT.c_authkey};
                c_timeOffset = PT.askInput(cl_questions, cl_defaults);
                PT.c_authkey = PT.checkChar(c_timeOffset, 'char', '^edu\.');

                if ~isempty(regexp(PT.c_authkey, 'edu\.jhu\.pha\.turbulence\.testing', 'once'))
                    error(cl_questions{1});
                end
            end

            result = [];
            
            fprintf('Requesting %s at %i points\n', method, i_points);

            % See if we have cache. If we do, return the cache. If not,
            % generate the request, return the fetched data and save the
            % cache
            if useCache
                cacheData = PT.RC.getCache();
                if ~isempty(cacheData)
                    result = cacheData;
                end
            end
            
            dbFunc = str2func(method);
            
            if isempty(result)
                
                if strcmp(method, 'getVelocity') || strcmp(method, 'getVelocityAndPressure') || strcmp(method, 'getPressure') || strcmp(method, 'getForce')
                    result = dbFunc(PT.c_authkey, PT.c_dataset, f_time, PT.c_spatialInt, PT.c_temporalInt, i_points, m_points);
                    if strcmp(method, 'getPressure')
                        result(1:3,:) = [];
                    end
                elseif strcmp(method, 'getVelocityGradient') || strcmp(method, 'getPressureHessian') || strcmp(method, 'getPressureGradient') || strcmp(method, 'getVelocityHessian') 
                    result = dbFunc(PT.c_authkey, PT.c_dataset, f_time, PT.c_spatialDiff, PT.c_temporalInt, i_points, m_points);
                    if strcmp(method, 'getPressureHessian')
                        %copy symmetric part
                        result(9,:) = result(6,:);
                        result(8,:) = result(5,:);
                        result(7,:) = result(3,:);
                        result(6,:) = result(5,:);
                        result(5,:) = result(4,:);
                        result(4,:) = result(2,:);
                    end
                end
                
                % save
                if useCache
                    PT.RC.saveCache(result);
                end
            end
        end
        
        % We want to fetch all parameters that define this type of
        % request, and create an md5 string of that        
        function string = createCacheFilename(~, type, parameters)
            
            string = type;
            for i = 1:numel(parameters)
                
                parameter = parameters{i};
                
                if isnumeric(parameter)
                    for j = 1:numel(parameter)
                        string = strcat(string, sprintf('-%1.4f', parameter(j)));
                    end
                end
                
                if ischar(parameter)
                    string = strcat(string, '-', parameter);
                end
            end
        end        

        %
        % ---- Data parse functions ----
        %
        
        % The next function calculates the vorticity. This function
        % requires the velocity gradients to be provided. The vorticity is
        % then calculated with: wx = dw/dy-dv/dz, wy = du/dz-dw/dx, wz =
        % dv/dx - du/dy
        function result = calculateVorticity(~, gradient)
            result(1,:) = gradient(8,:) - gradient(6,:);
            result(2,:) = gradient(3,:) - gradient(7,:);
            result(3,:) = gradient(4,:) - gradient(2,:);
        end
        
        % This function calculates the second invariant of the velocity
        % gradient, with Q = .5*(|omega|^2 - |S|^2)
        function result = calculateSecondInvariant(PT, gradient)
            [~, ~, SSt, OOt] = PT.calculateSymmetry(gradient);
            
            Q = (sqrt(OOt(1,1,:)+OOt(2,2,:)+OOt(3,3,:)).^2 - ...
                      sqrt(SSt(1,1,:)+SSt(2,2,:)+SSt(3,3,:)).^2)/2;
                  
            result = permute(Q, [3 1 2]);
        end
        
        % This function treats the input as a 3x3 tensor, N-times stacked.
        % However, the input is a 9xN matrix, which will get reshaped to a
        % 3x3xN 3d matrix.
        function result = calculateTrace(~, tensor)
            PHt = reshape(tensor, 3, 3, length(tensor));
            PH = permute(PHt, [2 1 3]);

            trace3d = arrayfun(@(ind) trace(PH(:,:,ind)), 1:size(PH,3), 'uniformOutput', false);
            result = cat(1, trace3d{:});
        end
            
        
        % Function that determines the lambda2 criterion. For this, we are
        % looking at the Eigenvalues of the tensor S^2+Omega^2, where S is
        % the symmetric part of the velocity gradient tensor, and Omega the
        % antisymmetric part. See Jinhee Jeong & Fazle Hussain 1995
        function result = calculateLambda2(PT, gradient)
            [S, ~, ~, ~, SdS, OdO] = PT.calculateSymmetry(gradient);
            
            eig3d = arrayfun(@(ind) eig(SdS(:,:,ind)+OdO(:,:,ind)), 1:size(S,3), 'uniformOutput', false);
            eigv = cat(3, eig3d{:});
            result = permute(eigv(2,1,:), [3 1 2]);
        end
        
        % Function to calculate symmetric- and antisymmetric parts of
        % tensor, and their squares. Input is a 9 by N matrix, where the 9
        % rows represent the 9 components of the tensor, and N the number
        % of points for which we have this tensor
        function [S O SSt OOt SdS OdO] =  calculateSymmetry(~, gradient)
            Jt = reshape(gradient, 3, 3, length(gradient));
            J = permute(Jt, [2 1 3]);
            S = (J+Jt)/2;
            O = (J-Jt)/2;
            St = permute(S, [2 1 3]);
            Ot = permute(O, [2 1 3]);
            
            multiply3dS = arrayfun(@(ind) S(:, :, ind) * St(:, :, ind), 1:size(S,3), 'uniformOutput', false);
            multiply3dO = arrayfun(@(ind) O(:, :, ind) * Ot(:, :, ind), 1:size(S,3), 'uniformOutput', false);
            SSt = cat(3, multiply3dS{:});
            OOt = cat(3, multiply3dO{:});
            
            SdS = zeros(3,3,length(S));
            OdO = zeros(3,3,length(O));
            for i = 1:3
                for k = 1:3
                    for j = 1:3
                        SdS(i,k,:) = SdS(i,k,:) + S(i,j,:) .* S(j,k,:);
                        OdO(i,k,:) = OdO(i,k,:) + O(i,j,:) .* O(j,k,:);
                    end
                end
            end
        end
        
        % Calculate the eigenvalues of the velocity gradient. If we have
        % complex values, than treat this point as part of a vortex
        function result = calculateComplexeEigenvalue(~, gradient)
            Jt = reshape(gradient, 3, 3, length(gradient));
            J = permute(Jt, [2 1 3]);            
            eig3d = arrayfun(@(ind) eig(J(:,:,ind)), 1:size(J,3), 'uniformOutput', false);
            eigv = cat(3, eig3d{:});
            result = abs(imag(permute(eigv(2,1,:), [3 1 2])));
        end
        
        % Function to calculate the vector magnitude. Simply use
        % Pythagoras to do so for every component. This function also
        % outputs the vector components in separate vectors. 
        % Of all four output variables, the index number corresponds to the
        % direction number. So in u(i,j,k) i->x, j->y and k->z. Be aware:
        % Matlab usually switches the first two indices in spatial data.
        function [u v w mag] = parseVector(~, m_results, m_nQueryPoints)
            
            magv = sqrt(m_results(1,:) .* m_results(1,:) + ...
                        m_results(2,:) .* m_results(2,:) + ...
                        m_results(3,:) .* m_results(3,:));
            
            mag = reshape(magv, m_nQueryPoints);
            u = reshape(m_results(1,:), m_nQueryPoints);
            v = reshape(m_results(2,:), m_nQueryPoints);
            w = reshape(m_results(3,:), m_nQueryPoints);
  
        end
        
        % This function grabs the velocity components and subtracts the
        % average per component. In this way we get a relative velocity
        % distribution with respoect to the average velocity
        function m_return = calculateRelativeVelocities(~, m_results)
            if size(m_results, 1) ~= 3
                error('The input matrix does not contain 3 velocity components');
            end
            
            m_return = zeros(size(m_results));
            meanComponent = zeros(1,3);
            for i=1:3
                meanComponent(i) = mean(m_results(i,:));
                m_return(i,:) = m_results(i,:) - meanComponent(i);
            end
            
            fprintf('Showing relative velocity components with respect to averages: u = %1.5f, v = %1.5f and w = %1.5f\n', meanComponent(1), meanComponent(2), meanComponent(3));
        end 
        
        % This function takes the created lines and the corresponding
        % linear vector for the requested velocity components, and returns
        % all the inline velocity components for all the lines, in a
        % structure. This structure now holds all the line signals
        function vInlineStruct = parseLines(~, results, s_lines)
            
            keys = fieldnames(s_lines);
            ind = 1;
            vInlineStruct = struct();
            for i = 1:numel(keys)
                key = char(keys(i));
                dir = s_lines.(key).dir;
                inc = numel(s_lines.(key).x);
                vInlineStruct.(key) = results(dir, ind:(ind+inc-1));
                ind = ind+inc;
            end
        end
        
        % Take all the signals in the line structure, and manipulate the
        % signals in such a way that the average becomes zero.
        function s_lines = calculateZeroMean(~, lines)
            keys = fieldnames(lines);
            s_lines = struct();
            for i = 1:numel(keys)
                key = char(keys(i));
                s_lines.(key) = lines.(key) - mean(lines.(key));
            end
        end
        
        % Once again, take all the signals on the lines, and calculate
        % statistical properties like mean, variance, mean squared and
        % standard deviation
        function [out] = calculateStatProperties(~, lines)
            keys = fieldnames(lines);
            out = zeros(1,4);
            for i = 1:numel(keys)
                key = char(keys(i));
                signal = lines.(key);
                out(1) = out(1) + mean(signal);
                out(2) = out(2) + var(signal);
                out(3) = out(3) + mean(signal.^2);
                out(4) = out(4) + sqrt(var(signal));
            end
            out = out./numel(keys);
        end
        
        % From the provided structure the signals are taken transformed to
        % the frequency domain with a Fast Fourier Transform method. The
        % presence of every frequency in a signal is averaged over all
        % provided signals
        function [dft pwr k n] = calculateFFTLines(~, s_inlineVel)
            
            keys = fieldnames(s_inlineVel);
            dft = zeros(1,1024);
            pwr = zeros(1,1024);
            for i = 1:numel(keys)
                key = char(keys(i));
                x = s_inlineVel.(key);          %signal
                m = length(x);                  %window length
                n = pow2(nextpow2(m));          %transform length
                y = fft(x,n);                   %DFT
                k = (0:n-1);                    %wave number
                power = y.*conj(y)/n;           %power of the DFT
                
                %collect
                dft = dft + abs(y);
                pwr = pwr + abs(power);
            end
            
            %average
            dft = 2*dft/numel(keys);
            pwr = 2*pwr/numel(keys);
        end        
        
        % This function scales the provided power spectrum with the
        % Kolmogorov length scale, dissipation rate and viscosity
        function [kEta E] = scaleEnergySpectrum(PT, k, pwr)
            
            kEta = k.*PT.KOLMOGOROV_LENGTH;
            E = pwr./(PT.DISSIPATION_RATE * PT.VISCOSITY^5)^(1/4);
            
        end
        
        % This function takes a signal and calculates the PDF of that
        % signal, for #steps number of bins. In addition, this function can
        % use an exponentially increasing (and decreasing) bin width, so
        % that bins at large distances from the mean have a large width.
        % This is particularly convenient when calculating the logarithm of
        % the PDF.
        function [x y avg rms vr std] = calculatePDF(PT, signal, steps, ratio, i_nondim, i_zeroMean)

            F = signal(:);
            avg = mean(F);
            
            % set mean to zero
            if i_zeroMean
                F = F - avg;
                newAvg = 0;
            else
                newAvg = avg;
            end
            
            rms = sqrt(sum(F.*conj(F))/numel(F));
            vr = var(F);
            std = sqrt(vr);            
            
            mn = min(F);
            mx = max(F);         
            
            if ratio == 1
                % output x axis
                x = linspace(mn, mx, steps);  
                space = ones(1, steps).*((mx-mn)/steps);
                
                % hist function
                PDF = hist(F, steps);                
            else
                if mod(steps, 2) == 0;
                    % even number
                    space = PT.factorspace(ratio, 1, steps/2);
                    space = [space flipdim(space, 2)];                      
                else
                    % odd number
                    space = PT.factorspace(ratio, 1, ceil(steps/2));
                    space = [space flipdim(space, 2)]; 
                    space(numel(space)/2+1) = []; 
                end
                
                % we want max accuracy at the mean
                if i_zeroMean
                    if abs(mx-newAvg) < abs(newAvg-mn)
                        mn = newAvg-abs(mx-newAvg);
                    else
                        mx = newAvg+abs(newAvg-mn);
                    end
                end
                
                % correct width
                f = sum(space) / (mx-mn);
                space = space./f;
                
            
                % we need one extra point, so that we have #steps intervals
                x = zeros(1, steps+1);
                for i = 1:numel(x)
                    if i == 1
                        x(i) = mn;
                    else
                        x(i) = x(i-1) + space(i-1);
                    end
                end
                
                % manual hist()
                PDF = zeros(1, steps);
                for i = 1:(numel(x)-1)
                    up = x(i+1);
                    dw = x(i);
                    PDF(i) = size(find(F >= dw & F < up), 1);
                end
                
                % correct for variable spacing
                linear = (mx-mn)/steps;
                PDF = (PDF./space)*linear;
                
                % get rid of last value for x
                x(end) = [];
                
            end

            % set the surface under the curve equal to 1
            surf = sum(PDF.*space);
            y = PDF./surf;
            
            % let's interpolate zeros
            %y = PT.interpolateZeros(y);
            
            % nondimensionalize with standard deviation. Std should equal
            % RMS for a zero mean signal
            if i_nondim
                x = x./std;
                y = y.*std;
            end
            
        end
        
        % This function takes a signal, searches for every series of zeros
        % and substitutes those zeros with a linear interpolation of the
        % non-zero neighbors
        function in = interpolateZeros(~, in)
            
            for i = 1:numel(in)
                
                if in(i) == 0
                    for j = (i+1):numel(in)
                        if in(j) > 0
                            up = in(j);
                            k = j-1;
                            break;
                        end
                    end
                    
                    % begin of signal
                    if i == 1
                        dw = up;
                    else
                        dw = in(i-1);
                    end
                    
                    % end of signal
                    if i == numel(in)
                        up = dw;
                        k = i;
                    end
                
                    in(i:k) = linspace(dw, up, (k-i)+1);
                end
            end
        end
        
        % This function takes a 3 dimensional array with single component
        % velocity data, and looks at the differences of two points in a
        % specified direction at a specified distance. It returns a linear
        % array of velocity increments, with on each row a different
        % increment size
        function [long trans] = calculateVelIncr(PT, u, v, w, m_spacing, f_rStart, f_rEnd, i_rSteps)

            long = struct();
            trans = struct();
            count = 0;
            
            vel(:,:,:,1) = u;
            vel(:,:,:,2) = v;
            vel(:,:,:,3) = w;
            
            % loop over different increments
            for incr = PT.factorspace(f_rStart, f_rEnd, i_rSteps);
                count = count+1;
                key = char(strcat('incr', num2str(count)));
                long.(key) = [];
                trans.(key) = [];
                
                % loop over velocity component
                for i = 1:3
                    
                    VV = vel(:,:,:,i);
                    
                    % loop over direction component
                    for j = 1:3
                        
                        incrSteps = round(incr/m_spacing(j));

                        % make sure we're not subtracting the same point
                        if incrSteps > 0

                            velIncr = [];
                            
                            % loop over different steps in direction
                            for k = 1:(size(VV, j)-incrSteps)
                                if j == 1
                                    df = VV(k+incrSteps, :, :) - VV(k, :, :);
                                elseif j == 2
                                    df = VV(:, k+incrSteps, :) - VV(:, k, :);
                                else
                                    df = VV(:, :, k+incrSteps) - VV(:, :, k);
                                end
                                
                                % stack linearly
                                velIncr = vertcat(velIncr, df(:)); %#ok<AGROW>
                            end

                            if i == j
                                long.(key) = vertcat(long.(key), velIncr);
                            else
                                trans.(key) = vertcat(trans.(key), velIncr);
                            end
                        end
                    end
                end
            end
        end

        %
        % ---- Drawing functions ----
        %
        
        % This function creates a meshgrid, with the provided number of
        % points per side. The npoints input variable can either have one
        % element or #nout elements
        function [X1 X2 X3] = meshgrid(~, m_nPoints, m_offsets, m_spacing)
            
            nout = max(nargout, 1);
            endpoints = (m_nPoints-1) .* m_spacing;

            if nout == 2
                x = linspace(0, endpoints(1), m_nPoints(1)) + m_offsets(1);
                y = linspace(0, endpoints(2), m_nPoints(2)) + m_offsets(2);
                [X1 X2] = meshgrid(x, y);
            elseif nout == 3
                x = linspace(0, endpoints(1), m_nPoints(1)) + m_offsets(1);
                y = linspace(0, endpoints(2), m_nPoints(2)) + m_offsets(2);                
                z = linspace(0, endpoints(3), m_nPoints(3)) + m_offsets(3);   
                [X1 X2 X3] = meshgrid(x, y, z);
            end            
        end        
        
        % This function starts up a figure with proper settings
        function x_figure = startFigure(~, id)
            x_figure = figure(id);
            m_screenSize = get(0, 'ScreenSize');
            set(x_figure, 'Position', [0 0 m_screenSize(3) m_screenSize(4) ] );
        end
        
        % Set two-dimensional or three-dimensional figure attributes
        function setFigureAttributes(PT, type, cl_labels)
            
            if strcmp(type, '1d')
                xlabel(cl_labels{1}, 'FontSize', 12, 'FontWeight', 'bold');
                ylabel(cl_labels{2}, 'FontSize', 12, 'FontWeight', 'bold');
                set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on');
                %axis equal;                        
            elseif strcmp(type, '2d')
                xlabel(cl_labels{1}, 'FontSize', 12, 'FontWeight', 'bold');
                ylabel(cl_labels{2}, 'FontSize', 12, 'FontWeight', 'bold');
                colorbar;
                colormap(PT.c_colormap);
                set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on');
                axis equal;
            elseif strcmp(type, '3d')
                xlabel(cl_labels{1}, 'FontSize', 12, 'FontWeight', 'bold');
                ylabel(cl_labels{2}, 'FontSize', 12, 'FontWeight', 'bold');
                zlabel(cl_labels{3}, 'FontSize', 12, 'FontWeight', 'bold');
                view(3);
                axis vis3d;
                camlight;
                lighting phong;
                axis equal;
                set(gca, 'FontSize', 11);
                set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on', 'ZMinorTick', 'on');
                grid;
                alpha(0.7);
            end
                
        end

        % Take the screen size, see what the smallest dimension is, and
        % give both figure dimensions this size.
        function makeFigureSquare(~, x_figure)
            
            m_screenSize = get(0, 'ScreenSize');
            if m_screenSize(3) > m_screenSize(4)
                max = m_screenSize(4);
            else
                max = m_screenSize(3);
            end
            
            % little bit of extra width because of colorbar
            set(x_figure, 'Position', [0 0 round(max*1.1) max]);
        end
        
        % Give a subplot the right dimensions, to use the space as
        % efficient as possible
        function scaleSubplot(~, num, x_subplot)

            width = 0.34;
            heigh = 0.39;
            left = 0.07;
            bottom = 0.07;
            midleft = 0.57;
            midbottom = 0.56;
            if num == 1
                set(x_subplot, 'pos', [left midbottom width heigh]);
            elseif num == 2
                set(x_subplot, 'pos', [midleft midbottom width heigh]);
            elseif num == 3
                set(x_subplot, 'pos', [left bottom width heigh]);
            else
                set(x_subplot, 'pos', [midleft bottom width heigh]);
            end

        end

        % This functions plots contours of a given matrix. It asumes that
        % the given matrix corresponds m_X1 and m_X2. Return a handle to
        % the contour
        function x_contour = drawCountours(~, var, m_X1, m_X2)

            x_contour = contourf(m_X1, m_X2, var, 30, 'LineStyle', 'none');
            
        end
        
        % Create a vector map here. This functions maps the provided vector
        % map onto a i_maxQuiver x i_maxQuiver grid.
        function x_quiver = drawVectormap(PT, var1, var2, m_X1, m_X2)

            % calculate new grid
            x = linspace(m_X1(1), m_X1(end), PT.i_maxQuiver);
            y = linspace(m_X2(1), m_X2(end), PT.i_maxQuiver);
            [m_X1n m_X2n] = meshgrid(x, y);
            
            % map vector components on new grid
            var1new = interp2(m_X1, m_X2, var1, m_X1n, m_X2n, 'linear');
            var2new = interp2(m_X1, m_X2, var2, m_X1n, m_X2n, 'linear');
            
            fprintf('Using an interpolated vector map grid of %ix%i points\n', size(m_X1n));
            
            x_quiver = quiver(m_X1n, m_X2n, var1new, var2new);
            
            % Set vector size and width
            set(x_quiver, 'AutoScaleFactor', 1.5);
            set(x_quiver, 'LineWidth', 1.1);
            set(x_quiver, 'Color', 'k');
        end        
        
        % Make an isosurface-patch image. Take a 3d scalar field, and plot
        % several semi-transparent iso-surfaces for this scalar. As input
        % we take <scalar field>, <scalar iso value start>, <start factor>,
        % <icrement>, <end factor>.
        % Also apply some interpolation for smooth surfaces
        function x_patch = drawIsoPatch(PT, scalar, m_X1, m_X2, m_X3, startf, endf, npoints)
            
            % Interpolate
            [scalar, m_X1, m_X2, m_X3] = PT.scalarInterp3(scalar, m_X1, m_X2, m_X3);
            fprintf('Using %s interpolation. Increased mesh to %ix%ix%i points\n', PT.ISOSURF_INTERP, size(m_X1));

            m_ones = ones(size(scalar));
            
            for i = linspace(startf, endf, npoints)
                
                % Check if this iso level exists
                if ~isempty(scalar(scalar>i))
                    [x_faces x_verts x_colors] = isosurface(m_X1, m_X2, m_X3, scalar, i, m_ones*i);

                    x_patch = patch('Vertices', x_verts, 'Faces', x_faces, ... 
                        'FaceVertexCData', x_colors, ...
                        'FaceColor','interp', ... 
                        'edgecolor', 'none');
                end
                
            end
            
            if npoints > 1
                colorbar;
                colormap 'Jet';
                colorbar('FontSize', 12);
            end

        end
        
        % This function grabs a 3d scalar field, and interpolates it to
        % 64^3 points
        function [scalar, m_X1, m_X2, m_X3] = scalarInterp3(PT, scalar, m_X1, m_X2, m_X3)
            
            % create new meshgrid only if we have to
            if size(m_X1, 1) < 64 || size(m_X1, 2) < 64 || size(m_X1, 3) < 64

                % create new field with 64 points on every side
                m_nPoints = 64*ones(1,3);
                m_offsets = [m_X1(1), m_X2(1), m_X3(1)];
                m_spacing = [(m_X1(end)-m_X1(1))/63, ...
                          (m_X2(end)-m_X2(1))/63, ...
                          (m_X3(end)-m_X3(1))/63];
                
                [m_X1n, m_X2n, m_X3n] = PT.meshgrid(m_nPoints, m_offsets, m_spacing);
                
                
                scalar = interp3(m_X1, m_X2, m_X3, scalar, m_X1n, m_X2n, m_X3n, PT.ISOSURF_INTERP);
                m_X1 = m_X1n; m_X2 = m_X2n; m_X3 = m_X3n;
            end
                
        end        
        
        % This function creates n evenly spaced colors, starting with green
        % and moving towards blue
        function colors = createColors(~, n)
            a = linspace(0,1,n);
            colors = zeros(n, 3);
            for i = 1:n
                % color range: green to blue
                colors(i,:) = [0 a(i) 1-a(i)];
                pause(rand/10);
            end
        end
        
        % Take 3 min and 3 max values, and use those to create a 3d block
        % with the surf() function
        function drawBlock(~, mn, mx, color)
            
            mnmx = horzcat(mn, mx);
            
            for i = 1:3
                for j = 1:2
                    ind = zeros(3,4);
                    ind(i,1:4) = mnmx(i,j);
                    k = setxor(1:3, i);
                    ind(k(1), 1:4) = [mn(k(1)) mn(k(1)) mx(k(1)) mx(k(1))];
                    ind(k(2), 1:4) = [mn(k(2)) mx(k(2)) mx(k(2)) mn(k(2))];
                    patch(ind(1,:), ind(2,:), ind(3,:), color);
                end
            end 
        end
        
        %
        % ---- Video functions ----
        %

        % Start a video
        function x_video = startVideo(~, x_figure)
            
            x_video = avifile('output.avi');
            x_video.fps = 2;
            x_video.quality = 100;

            set(x_figure, 'NextPlot', 'replacechildren');
        end
        
        % Next frame in video
        function x_video = saveVideo(~, x_figure, x_video)
            
            % Store video
            frame = getframe(x_figure);
            x_video = addframe(x_video, frame);
            
        end
        
        %
        % ---- 1D line functions ----
        %
        
        % Construct random lines that are bounded by the domain boundaries.
        % To do this, we pick two random points on the boundary and connect
        % these with a line. The lines contain evenly spaced points with a
        % spacing of PT.SPACING
        function [lines i_points] = createLines(PT, i_lines)
            
            lines = struct();
            i_points = 0;

            for l = 1:i_lines
                %select random start point
                p = zeros(2,3);
                ind = 1:3;
                a = floor(rand*3)+1;
                ind = setxor(ind,a);
                p(1,a) = 0;
                p(1,ind) = rand(1,2)*1024;
                
                %select end point
                p(2,:) = p(1,:);
                p(2,a) = 1024;

                %store line in struct
                i_points = i_points+1024;
                key = char(strcat('line', num2str(l)));
                lines.(key).x = linspace(p(1,1), p(2,1), 1024) * PT.SPACING;
                lines.(key).y = linspace(p(1,2), p(2,2), 1024) * PT.SPACING;
                lines.(key).z = linspace(p(1,3), p(2,3), 1024) * PT.SPACING;
                lines.(key).dir = a;
            end
        end
        
        % This function creates a 3 x i_points matrix with on the rows the
        % x, y and z coordinates of the points in the lines. In order to
        % only perform one request on the database, all lines are stacked
        % linearly
        function m_points = fillLines(~, s_lines)
            
            m_points = [];
            keys = fieldnames(s_lines);
            
            for i = 1:numel(keys)
                key = char(keys(i));
                m_points = horzcat(m_points, [s_lines.(key).x; s_lines.(key).y; s_lines.(key).z;]);  %#ok<AGROW>
            end
        end 
    end    
end

