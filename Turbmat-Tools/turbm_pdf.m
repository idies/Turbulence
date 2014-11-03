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

TT = TurbTools(0);

% Number of steps in the PDF histogram
i_pdfBins = 60;

%
% ---- User input ----
%

% Timestep
cl_questions = {sprintf('Enter timestep between 1 and %i (integer)', TT.TIME_OFFSET_MAX)};
cl_defaults = {'100'};
c_timeOffset = TT.askInput(cl_questions, cl_defaults);
i_timeOffset = TT.checkChar(c_timeOffset, 'int', '', [0 1024]);

% Cube dimensions
cl_questions = {sprintf('Cube size in number of grid points')};
cl_defaults = {'128'};
c_cubeWidth = TT.askInput(cl_questions, cl_defaults);
i_cubeWidth = TT.checkChar(c_cubeWidth, 'int', '', [0 1024]);

m_nPoints = i_cubeWidth * ones(1,3); 
[m_nQueryPoints m_spacing] = TT.calculateQueryPoints(m_nPoints);

% Nondimensionalized axis
c_nondim = TT.askYesno('Do you want to use a nondimensionalized axis?', 'Yes');
c_nondim = TT.checkChar(c_nondim, 'char', '^Yes|No$');
if strcmp(c_nondim, 'Yes'); i_nondim = 1; else i_nondim = 0; end

% Choose random offset
m_offsets = rand(1,3)*2*pi;

%
% ---- Construct the request ----
%

f_time = TT.TIME_SPACING * i_timeOffset;
i_points = prod(m_nQueryPoints);

fprintf('Querying %i points of a %ix%ix%i cube at randomly chosen position x=%1.4f, y=%1.4f, z=%1.4f\n', i_points, m_nPoints, m_offsets);

m_points = TT.fillBlock(m_nQueryPoints, m_offsets, m_spacing);
m_result4 = TT.callDatabase('getVelocityAndPressure', i_points, m_points, f_time, 0);

m_colors = TT.createColors(3);

%
% ---- Calculate Velocity PDF ----
%

x_figure = TT.startFigure(1);
subplot(2,2,[1 2]);
x_bar = zeros(1,3);
x_plot = zeros(1,3);

% three velocity components
for i = 1:3
    
    [x PDF] = TT.calculatePDF(m_result4(i,:), i_pdfBins, 1, i_nondim, 0);
    x_bar(i) = bar(x, PDF, 'EdgeColor', m_colors(i,:), 'FaceColor', m_colors(i,:));
    x_obj = findobj(gca, 'Type', 'patch');
    set(x_obj, 'facealpha', 1-(i-1)/4);
    hold on;
    
    % draw a nice outline
    x_plot(i) = plot(x, PDF, 'k', 'LineWidth', 1.3);  
    
end

% Style figure
grid;
ylabel('Pdf(v_i)', 'FontSize', 12, 'FontWeight', 'bold');
if i_nondim; xlabel('v_i/{\sigma_{v_i}}', 'FontSize', 12, 'FontWeight', 'bold'); else xlabel('V_i', 'FontSize', 12, 'FontWeight', 'bold'); end
legend([x_bar, x_plot(1)], 'v_x', 'v_y', 'v_z', 'outline');
title(sprintf('PDF of velocity in {%3i}^3 cube', i_cubeWidth), 'FontSize', 12, 'FontWeight', 'bold');
set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on');

%
% ---- Calculate Pressure PDF ----
%

% 4th row of m_result4 is pressure
pressure = m_result4(4,:);

subplot(2,2,3);

% one pressure component
[x PDF avg] = TT.calculatePDF(pressure, i_pdfBins, 1, i_nondim, 1);
plot(x, PDF, 'Color', m_colors(1,:), 'LineWidth', 1.3);

% Style figure
grid;
ylabel('Pdf(p)', 'FontSize', 12, 'FontWeight', 'bold');
if i_nondim; xlabel('p/{\sigma_{p}}', 'FontSize', 12, 'FontWeight', 'bold'); else xlabel('p', 'FontSize', 12, 'FontWeight', 'bold'); end
title(sprintf('PDF of pressure\nMean set to zero (real mean = %1.4f)', avg), 'FontSize', 12, 'FontWeight', 'bold');
set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on');
TT.makeFigureSquare(x_figure);

%
% --- Calculate Velocity Gradient PDF ---
%

m_result9 = TT.callDatabase('getVelocityGradient', i_points, m_points, f_time, 0);

subplot(2,2,4);
i_pdfBins = 40;

% transverse and longitudinal gradient components
m_tvGrad = m_result9([2 3 4 6 7 8], :);
m_ltGrad = m_result9([1 5 9], :);
[m_tv m_tvGradPDF] = TT.calculatePDF(m_tvGrad, i_pdfBins, 10, i_nondim, 1);
[m_lt m_ltGradPDF] = TT.calculatePDF(m_ltGrad, i_pdfBins, 10, i_nondim, 1);

plot(m_lt, log10(m_ltGradPDF), 'Color', m_colors(1,:), 'LineWidth', 1.3); hold on;
plot(m_tv, log10(m_tvGradPDF), 'Color', m_colors(end,:), 'LineWidth', 1.3);
plot(m_lt, log10(m_ltGradPDF), 'r.', 'LineWidth', 1.3);
plot(m_tv, log10(m_tvGradPDF), 'r.', 'LineWidth', 1.3);

% Style figure
grid;
ylabel('Pdf(J_{i,j})', 'FontSize', 12, 'FontWeight', 'bold');
if i_nondim; xlabel('{J_{i,j}}/{\sigma_{J_{i,j}}}', 'FontSize', 12, 'FontWeight', 'bold'); else xlabel('{J_{i,j}}', 'FontSize', 12, 'FontWeight', 'bold'); end
legend('Longitudinal J_{i,i}', 'Transverse J_{i,j}, i <> j', 'Location', 'NorthWest');
title('PDF of J_{i,j} = {\delta}v_i/{\delta}x_j', 'FontSize', 12, 'FontWeight', 'bold');
set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on');
TT.makeFigureSquare(x_figure);
