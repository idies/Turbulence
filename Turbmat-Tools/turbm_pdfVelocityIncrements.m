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

% Start the class
TT = TurbTools(0);
TT.c_spatialInt = TT.LAG8_INT;

% Set spacing of r
f_rStart = 2*pi/1024;
f_rEnd = 2*pi/4;
i_rSteps = 12;

% Number of steps in the PDF histogram
i_pdfBins = 40;

% Ratio of outer bins with inner bins
f_pdfBinRatio = 8.0;

%
% ---- User input ----
%

% Timestep
cl_questions = {sprintf('Enter timestep between 1 and %i (integer)', TT.TIME_OFFSET_MAX)};
cl_defaults = {'100'};
c_timeOffset = TT.askInput(cl_questions, cl_defaults);
i_timeOffset = TT.checkChar(c_timeOffset, 'int', '', [0 1024]);

% Number of cubes to fetch
cl_questions = {sprintf('How many cubes do we want to fetch?')};
cl_defaults = {'6'};
c_nCubes = TT.askInput(cl_questions, cl_defaults);
i_nCubes = TT.checkChar(c_nCubes, 'int', '', [1 100]);

% Cube dimensions
m_randDimensions = [32 128 512];

%
% ---- Construct the request ----
%

f_time = TT.TIME_SPACING * i_timeOffset;
fprintf('Querying %i %ix%ix%i-cubes with random position and orientation\n', i_nCubes, m_randDimensions);

% Create legends
r = TT.factorspace(f_rStart, f_rEnd, i_rSteps);
for i = 1:i_rSteps
    eta = r(i)/TT.KOLMOGOROV_LENGTH;
    legIncr{i} = strcat(sprintf('r = %.2f', eta), ' \eta'); %#ok<SAGROW>
end

m_colors = TT.createColors(i_nCubes);

%
% ---- Collect random blocks ----
%

TT.startFigure(1);
subplot(2,2,[1; 3]);

for step = 1:i_nCubes
    
    key = char(strcat('cube', num2str(step)));
    
    fprintf('Cube #%i: ', step);
    
    % Create random block
    m_nPoints = m_randDimensions(randperm(3));
    [m_nQueryPoints m_spacing] = TT.calculateQueryPoints(m_nPoints);
    i_points = prod(m_nQueryPoints);    
    m_offsets = rand(1,3)*2*pi;
    m_points = TT.fillBlock(m_nQueryPoints, m_offsets, m_spacing);
    
    % Call the database
    m_result4 = TT.callDatabase('getVelocity', i_points, m_points, f_time, 0);
    
    % Calculate Velocity increments
    [v_u v_v v_w] = TT.parseVector(m_result4, m_nQueryPoints);
    [s_lt_temp s_tv_temp] = TT.calculateVelIncr(v_u, v_v, v_w, m_spacing, f_rStart, f_rEnd, i_rSteps);
    
    % Add to previously queried blocks
    if step == 1
        s_lt = s_lt_temp;
        s_tv = s_tv_temp;
    else
        keys = fieldnames(s_lt_temp);
        for i = 1:numel(keys)
            key = char(keys(i));
            s_lt.(key) = vertcat(s_lt.(key), s_lt_temp.(key));
            s_tv.(key) = vertcat(s_tv.(key), s_tv_temp.(key));
        end
    end
    
    % Draw this block
    TT.drawBlock(min(m_points, [], 2), max(m_points, [], 2), m_colors(step,:)); hold on;

end

% Style figure
TT.setFigureAttributes('3d', {'x', 'y', 'z'});
axis([0 2*pi 0 2*pi 0 2*pi]);
title('Randomly selected blocks', 'FontSize', 12, 'FontWeight', 'bold');

%
% ---- Calculate statistics ----
%

keys = fieldnames(s_lt);
s_ltPDF = struct();
s_tvPDF = struct();
count = 0;
for i = 1:numel(keys)
    key = char(keys(i));
    
    if numel(s_lt.(key)) > 0 && numel(s_tv.(key)) > 0
        count = count+1;
        
        % PDF
        [s_ltPDF.(key).x, s_ltPDF.(key).y] = TT.calculatePDF(s_lt.(key), i_pdfBins, f_pdfBinRatio, 1, 1);
        [s_tvPDF.(key).x, s_tvPDF.(key).y] = TT.calculatePDF(s_tv.(key), i_pdfBins, f_pdfBinRatio, 1, 1);

        % Structure function
        m_ltSF(count) = mean(s_lt.(key).^2); %#ok<SAGROW>
        m_tvSF(count) = mean(s_tv.(key).^2); %#ok<SAGROW>

        % 3rd moment
        m_lt3rd(count) = moment(s_lt.(key), 3); %#ok<SAGROW>
        m_tv3rd(count) = moment(s_tv.(key), 3); %#ok<SAGROW>
        
        % Skewness
        m_ltS(count) = skewness(s_lt.(key)); %#ok<SAGROW>
        m_tvS(count) = skewness(s_tv.(key)); %#ok<SAGROW>

        % Kurtosis
        m_ltK(count) = kurtosis(s_lt.(key)); %#ok<SAGROW>
        m_tvK(count) = kurtosis(s_tv.(key)); %#ok<SAGROW>
        
    else
        
        % unset this r, because it has zero values
        r(i) = [];
        legIncr(i) = []; %#ok<SAGROW>

    end
end

%
% ---- Plot PDF of first signal of every mode ----
%

subplot(2,2,2);

keys = fieldnames(s_ltPDF);
firstkey = keys{1};

bar(s_ltPDF.(firstkey).x, s_ltPDF.(firstkey).y, 1, 'FaceColor', m_colors(1,:), 'EdgeColor', m_colors(1,:)); hold on;
plot(s_ltPDF.(firstkey).x, s_ltPDF.(firstkey).y, 'k', 'LineWidth', 1.3);

% Style figure
grid;
ylabel('Pdf({\delta}_{r}v)', 'FontSize', 12, 'FontWeight', 'bold');
xlabel('{\delta}_{r}v/{\sigma_{{\delta}_{r}v}}', 'FontSize', 12, 'FontWeight', 'bold');
title(strcat(sprintf('PDF of longitudinal velocity increments at r = %.2f', f_rStart/TT.KOLMOGOROV_LENGTH), ' \eta'), 'FontSize', 12, 'FontWeight', 'bold');
set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on');

subplot(2,2,4);

bar(s_tvPDF.(firstkey).x, s_tvPDF.(firstkey).y, 1, 'FaceColor', m_colors(end,:), 'EdgeColor', m_colors(end,:)); hold on;
plot(s_tvPDF.(firstkey).x, s_tvPDF.(firstkey).y, 'k', 'LineWidth', 1.3);

% Style figure
grid;
ylabel('Pdf({\delta}_{r}v)', 'FontSize', 12, 'FontWeight', 'bold');
xlabel('{\delta}_{r}v/{\sigma_{{\delta}_{r}v}}', 'FontSize', 12, 'FontWeight', 'bold');
title(strcat(sprintf('PDF of transverse velocity increments at r = %.2f', f_rStart/TT.KOLMOGOROV_LENGTH), ' \eta'), 'FontSize', 12, 'FontWeight', 'bold');
set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on');

% Create new colors
m_colors = TT.createColors(i_rSteps);

%
% ---- Plot log(PDF) of all longitudinal signals ----
%

TT.startFigure(2);
subplot(1,2,1);

keys = fieldnames(s_ltPDF);
x_plot = zeros(1,4);
for i = 1:numel(keys)
    key = char(keys(i));
    x_plot(i) = plot(s_ltPDF.(key).x, log10(s_ltPDF.(key).y), 'Color', m_colors(i,:), 'LineWidth', 1.3); hold on;
end

% Style figure
grid;
ylabel('log(Pdf({\delta}_{r}v))', 'FontSize', 12, 'FontWeight', 'bold');
xlabel('{\delta}_{r}v/{\sigma_{{\delta}_{r}v}}', 'FontSize', 12, 'FontWeight', 'bold');
legend([x_plot(1) x_plot(end)], legIncr{1}, legIncr{end});
title('Logarithmic PDF of longitudinal velocity increments', 'FontSize', 12, 'FontWeight', 'bold');
set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on');

%
% ---- Plot log(PDF) of all transverse signals ----
%
    
subplot(1,2,2);
keys = fieldnames(s_tvPDF);
x_plot = zeros(1,4);
for i = 1:numel(keys)
    key = char(keys(i));
    x_plot(i) = plot(s_tvPDF.(key).x, log10(s_tvPDF.(key).y), 'Color', m_colors(i,:), 'LineWidth', 1.3); hold on;
end

% Style figure
grid;
ylabel('log(Pdf({\delta}_{r}v))', 'FontSize', 12, 'FontWeight', 'bold');
xlabel('{\delta}_{r}v/{\sigma_{{\delta}_{r}v}}', 'FontSize', 12, 'FontWeight', 'bold');
legend([x_plot(1) x_plot(end)], legIncr{1}, legIncr{end});
title('Logarithmic PDF of transverse velocity increments', 'FontSize', 12, 'FontWeight', 'bold');
set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on');

%
% ---- Plot structure function ----
%

TT.startFigure(3);
rnd = r./TT.KOLMOGOROV_LENGTH;

subplot(2,2,1);

x_plot = zeros(1,4);
x_plot(1) = loglog(rnd, m_ltSF, 'Color', m_colors(1,:), 'LineWidth', 1.3); hold on;
loglog(rnd, m_ltSF, 'r.', 'LineWidth', 1.3);
x_plot(2) = loglog(rnd, m_tvSF, 'Color', m_colors(end,:), 'LineWidth', 1.3);
loglog(rnd, m_tvSF, 'r.', 'LineWidth', 1.3);

yy = 2.1*TT.DISSIPATION_RATE^(2/3) * r.^(2/3);
yy2 = (4/3) .* yy;

x_plot(3) = loglog(rnd, yy, 'r', 'LineWidth', 1.3);
x_plot(4) = loglog(rnd, yy2, 'r--', 'LineWidth', 1.3);

% Style figure
grid;
ylabel('{{\sigma}_r}^2', 'FontSize', 12, 'FontWeight', 'bold');
xlabel('r/{\eta}', 'FontSize', 12, 'FontWeight', 'bold');
legend(x_plot, 'Longitudinal', 'Transverse', '2.1 * {\epsilon}^{2/3} r^{2/3}', '4/3 * 2.1 * {\epsilon}^{2/3} r^{2/3}');
title('Structure function', 'FontSize', 12, 'FontWeight', 'bold');
set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on');

%
% ---- Plot 3rd moment ----
%

subplot(2,2,2);

x_plot = zeros(1,2);
x_plot(1) = semilogx(rnd, m_lt3rd, 'Color', m_colors(1,:), 'LineWidth', 1.3); hold on;
semilogx(rnd, m_lt3rd, 'r.', 'LineWidth', 1.3);
x_plot(2) = semilogx(rnd, m_tv3rd, 'Color', m_colors(end,:), 'LineWidth', 1.3);
semilogx(rnd, m_tv3rd, 'r.', 'LineWidth', 1.3);

% Style figure
grid;
ylabel('{{\mu}_r}^3', 'FontSize', 12, 'FontWeight', 'bold');
xlabel('r/{\eta}', 'FontSize', 12, 'FontWeight', 'bold');
legend(x_plot, 'Longitudinal', 'Transverse');
title('Third central moment', 'FontSize', 12, 'FontWeight', 'bold');
set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on');

%
% ---- Plot skewness ----
%

subplot(2,2,3);

x_plot = zeros(1,2);
x_plot(1) = semilogx(rnd, -m_ltS, 'Color', m_colors(1,:), 'LineWidth', 1.3); hold on; 
semilogx(rnd, -m_ltS, 'r.', 'LineWidth', 1.3);
x_plot(2) = semilogx(rnd, -m_tvS, 'Color', m_colors(end,:), 'LineWidth', 1.3);
semilogx(rnd, -m_tvS, 'r.', 'LineWidth', 1.3);

% Style figure
grid;
ylabel('-{{S}_r}', 'FontSize', 12, 'FontWeight', 'bold');
xlabel('r/{\eta}', 'FontSize', 12, 'FontWeight', 'bold');
legend(x_plot, 'Longitudinal', 'Transverse');
title('Skewness', 'FontSize', 12, 'FontWeight', 'bold');
set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on');

%
% ---- Plot kurtosis ----
%

subplot(2,2,4);

x_plot = zeros(1,2);
x_plot(1) = semilogx(rnd, m_ltK, 'Color', m_colors(1,:), 'LineWidth', 1.3); hold on; 
semilogx(rnd, m_ltK, 'r.', 'LineWidth', 1.3);
x_plot(2) = semilogx(rnd, m_tvK, 'Color', m_colors(end,:), 'LineWidth', 1.3);
semilogx(rnd, m_tvK, 'r.', 'LineWidth', 1.3);

% Style figure
grid;
ylabel('{{K}_r}', 'FontSize', 12, 'FontWeight', 'bold');
xlabel('r/{\eta}', 'FontSize', 12, 'FontWeight', 'bold');
legend(x_plot, 'Longitudinal', 'Transverse');
title('Kurtosis', 'FontSize', 12, 'FontWeight', 'bold');
set(gca, 'TickDir', 'out', 'TickLength', [.02 .02],'XMinorTick', 'on', 'YMinorTick', 'on');
