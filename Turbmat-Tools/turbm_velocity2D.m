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

%
% ---- Ask user input ----
%

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
       
% Surface normal direction, and also compute the two surface directions
cl_questions = {sprintf('Enter surface normal direction (x, y or z)')};
cl_defaults = {'z'};
c_surfDirection = TT.checkChar(TT.askInput(cl_questions, cl_defaults), 'char', '^[xyz]{1}$');
c_directions = TT.setDirections(c_surfDirection);

% Surface dimensions (two input fields here)
cl_questions = {sprintf('Number of grid points in %c', c_directions(1)),...
                sprintf('Number of grid points in %c', c_directions(2))};
cl_defaults = {'64','64'};
cl_nPoints = TT.askInput(cl_questions, cl_defaults);
m_nPoints = TT.checkCellChars(cl_nPoints, [2 3], 'int', '', [0 1024]);
[m_nQueryPoints m_spacing] = TT.calculateQueryPoints(m_nPoints);

% Offset position
cl_questions = {sprintf('Offset in %c', c_directions(1)),...
                sprintf('Offset in %c', c_directions(2)),...
                sprintf('Offset in %c', c_surfDirection)};
cl_defaults = {'1.0','1.0','1.0'};
cl_offsets = TT.askInput(cl_questions, cl_defaults);
m_offsets = TT.checkCellChars(cl_offsets, [2 3], 'float', '');

% Display relative velocity?
c_relative = TT.askYesno('Calculate relative velocity?', 'No');
c_relative = TT.checkChar(c_relative, 'char', '^Yes|No$');
if strcmp(c_relative, 'Yes'); i_relative = 1; else i_relative = 0; end

%
% ---- Construct the request ----
%

f_time = TT.TIME_SPACING * i_timeOffset;
i_points = prod(m_nQueryPoints);

m_points = TT.fillRectangle(m_nQueryPoints, m_offsets, m_spacing, c_directions);


% Timestep loop
for i_timeStep = 1:i_timeSteps

    fprintf('Time step %i of %i, t = %1.4f\n', i_timeStep, i_timeSteps, f_time);
    
    cl_cacheParams = {TT.c_dataset, f_time, TT.c_spatialInt, TT.c_temporalInt, TT.c_spatialDiff, i_points, m_nQueryPoints, m_points(:,1), m_points(:,end)};
    TT.RC.cacheFilename = TT.createCacheFilename('getVelocity', cl_cacheParams);    
    m_result3 =  TT.callDatabase('getVelocity', i_points, m_points, f_time, 1);
    
    % Calculate relative velocity
    if i_relative
        m_result3 = TT.calculateRelativeVelocities(m_result3);
    end    
    
    %
    % --- Plot the velocity ---
    %
    
    % Gives transposed c_directions(1) and c_directions(2) dimensions (so
    % m_X1(c_directions(2),c_directions(1)), etc)
    [m_X1 m_X2] = TT.meshgrid(m_nQueryPoints, m_offsets, m_spacing);
    
    % Gives non-transposed c_directions(1) and c_directions(2) dimensions
    % (so v_u(c_directions(1),c_directions(2)), etc)
    [v_u v_v v_w v_mag] = TT.parseVector(m_result3, m_nQueryPoints);

    if i_timeStep == 1
        x_figure = TT.startFigure(1);

        if i_timeSteps > 1
            x_video = TT.startVideo(x_figure);
        end 
    else
        clf(x_figure, 'reset');
    end

    % Tranpose velocity, to agree with spatial data. So:
    % v_u(c_directions(1),c_directions(2))
    v_u = transpose(v_u); v_v = transpose(v_v); v_w = transpose(v_w);
    
    TT.drawCountours(v_w, m_X1, m_X2); hold on;
    TT.drawVectormap(v_u, v_v, m_X1, m_X2);
    
    % Style figure
    TT.setFigureAttributes('2d', {c_directions(1), c_directions(2)});
    axis([m_X1(1), m_X1(end), m_X2(1), m_X2(end)]);
    title(sprintf('Velocity vectormap on surface facing %c-direction. The colormap indicates %c-component of velocity.\nTime = %1.4f', c_surfDirection, c_surfDirection, f_time), 'FontSize', 12, 'FontWeight', 'bold');
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
