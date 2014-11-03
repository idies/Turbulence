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


%
% ---- Initiate ----
%

clear all;
close all;
beep off;
clc;

TT = TurbTools(1);
TT.c_spatialDiff = TT.FD4_DIFF_LAG4_INT;

%
% ---- Ask user input ----
%

% Method
cl_questions = {'Select display method'};
cl_options = {'Iso-vorticity surfaces', ...
              'Q-criterion', ...
              'Lambda2 criterion', ...
              'Complex Eigenvalues', ...
              'Pressure Hessian Trace'};
c_vortMethod = TT.askOptions(cl_questions, cl_options);
i_vortMethod = TT.checkChar(c_vortMethod, 'int','^[1-5]$');

% Timestep
cl_questions = {sprintf('Enter timestep between 1 and %i (integer)', TT.TIME_OFFSET_MAX)};
cl_defaults = {'100'};
c_timeOffset = TT.askInput(cl_questions, cl_defaults);
i_timeOffset = TT.checkChar(c_timeOffset, 'int', '', [0 1024]);

% Number of timesteps to run
cl_questions = {sprintf('Enter number of timesteps to run, between 1 and %i (integer)', (TT.TIME_OFFSET_MAX-i_timeOffset))};
cl_defaults = {'1'};
c_timeSteps = TT.askInput(cl_questions, cl_defaults);
i_timeSteps = TT.checkChar(c_timeSteps, 'int', '', [0 1024-i_timeOffset]);

% Volume dimensions
m_nPoints = [64 64 64]; % Dimensions of cube
[m_nQueryPoints m_spacing] = TT.calculateQueryPoints(m_nPoints);

% Offset position
cl_questions = {sprintf('X-offset'),...
                sprintf('Y-offset'),...
                sprintf('Z-offset')};
cl_defaults = {'0.0','0.0','0.0'};
cl_offsets = TT.askInput(cl_questions, cl_defaults);
m_offsets = TT.checkCellChars(cl_offsets, [3 3], 'float', '');

%
% ---- Construct the request ----
%

f_time = TT.TIME_SPACING * i_timeOffset;
i_points = prod(m_nQueryPoints);

m_points = TT.fillBlock(m_nQueryPoints, m_offsets, m_spacing);

% Timestep loop
for i_timeStep = 1:i_timeSteps
    
    fprintf('Time step %i of %i, t = %1.4f\n', i_timeStep, i_timeSteps, f_time);

    cl_cacheParams = {TT.c_dataset, f_time, TT.c_spatialInt, TT.c_temporalInt, TT.c_spatialDiff, i_points, m_nQueryPoints, m_points(:,1), m_points(:,end)};
    if i_vortMethod == 5
        % Get pressure
        TT.RC.cacheFilename = TT.createCacheFilename('getPressureHessian', cl_cacheParams);
        m_pressure9 = TT.callDatabase('getPressureHessian', i_points, m_points, f_time, 1);
    else
        % Get velocity gradient
        TT.RC.cacheFilename = TT.createCacheFilename('getVelocityGradient', cl_cacheParams);
        m_result9 =  TT.callDatabase('getVelocityGradient', i_points, m_points, f_time, 1);
    end

    %
    % ---- Calculate scalar field ----
    %
    
    if i_vortMethod == 1 % Normal
        
        m_vorticity3 = TT.calculateVorticity(m_result9);    
        [w_x w_y w_z ISO] = TT.parseVector(m_vorticity3, m_nQueryPoints);
        
    elseif i_vortMethod == 2 % Q-value
        
        m_Qvalue = TT.calculateSecondInvariant(m_result9);
        ISO = reshape(m_Qvalue, m_nQueryPoints);
        
    elseif i_vortMethod == 3 % Lambda-2 criterion
        
        m_lambda2 = TT.calculateLambda2(m_result9);
        ISO = reshape(m_lambda2, m_nQueryPoints);
        ISO = -1*ISO;
        
    elseif i_vortMethod == 4 % Complex eigenvalue criterium
        
        m_complex = TT.calculateComplexeEigenvalue(m_result9);
        ISO = reshape(m_complex, m_nQueryPoints);
    
    elseif i_vortMethod == 5 % Pressure Hessian Trace
    
        m_pressureHesTrace = TT.calculateTrace(m_pressure9);
        ISO = reshape(m_pressureHesTrace, m_nQueryPoints);
        
    end
    
    % Gives permuted x and y dimensions (so m_X1(y,x,z), etc)
    [m_X1 m_X2 m_X3] = TT.meshgrid(m_nQueryPoints, m_offsets, m_spacing);

    %
    % ---- Plot isosurfaces ----
    %    
    
    % Start display of figure just after first database call
    if i_timeStep == 1
        x_figure = TT.startFigure(1);
        
        if i_timeSteps > 1
            x_video = TT.startVideo(x_figure);
        end
    else
        clf(x_figure, 'reset');
    end
    
    % Show isosurfaces
    hold on;
    avg = mean(ISO(ISO>0));
    
    % Permute x and y, to agree with spatial data. So: ISO(y,x,z)
    ISO = permute(ISO, [2 1 3]); 
    x_patch = TT.drawIsoPatch(ISO, m_X1, m_X2, m_X3, 2.5*avg, 6*avg, 3);
    
    % Style figure
    TT.setFigureAttributes('3d', {'x', 'y', 'z'});
    title(sprintf('%s iso-surfaces', cl_options{i_vortMethod}), 'FontSize', 13, 'FontWeight', 'bold');
    TT.makeFigureSquare(x_figure);

    % Process video and move to next time step
    if i_timeSteps > 1
        x_video = TT.saveVideo(x_figure, x_video);
        f_time = f_time + 2*TT.TIME_SPACING;
    end

end

if i_timeSteps > 1
    x_video = close(x_video);
end
