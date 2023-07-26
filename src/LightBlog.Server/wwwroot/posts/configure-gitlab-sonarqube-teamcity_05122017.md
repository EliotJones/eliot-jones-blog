# Configuring SonarQube with GitLab and TeamCity #

Introducing static analysis to a project can help inform code reviews and highlight
areas of the code likely to cause errors as well as expose trends in code
quality over time. The tradeoff is that there are often many false positives
in a report which need to be investigated.

When I configured [SonarQube][link1] (6.4) to provide static analysis for our C# project we struggled to incorporate
it into our normal development process since it sat outside the usual
branch -> build -> merge request workflow.

For our source control we were using GitLab (10.1.4) and our build server
was running TeamCity (2017.1).

### Get the plugin ###

Gabriel Allaigre has written the [sonar-gitlab plugin](https://gitlab.talanlabs.com/gabriel-allaigre/sonar-gitlab-plugin "GitLab page for plugin") which enables SonarQube to push its analysis results to GitLab. This presents the results of analysis in the same place we review our merge requests as well as causing build errors when violations occur; and therefore helps incorporate SonarQube into the development workflow.

First you will need to install the sonar-gitlab plugin to your SonarQube environment and follow the steps detailed in the configuration section of the readme:

1. Set the GitLab URL from the Administration -> Configuration -> General Settings -> GitLab
2. Set the GitLab user token in the same place. This should be a token for a GitLab user with the developer role. You can get this token in GitLab by going to Profile -> Edit Profile -> Access Tokens and generating a new access token.

Once this is installed the SonarQube configuration is complete.

### Configure TeamCity ###

The installation guide for the sonar-gitlab plugin describes how to configure it when using the GitLab CI or Maven for builds. To run the analysis from TeamCity we need to get some additional information for the parameters to the command line.

If we were running from GitLab's CI we would use the following command to start the Sonar MSBuild Scanner, pushing to GitLab after the analysis completed:

    SonarQube.Scanner.MSBuild begin /k:"$env:ProjName" /n:"$env:ProjName" /v:"1.0" /d:sonar.gitlab.project_id=$CI_PROJECT_ID /d:sonar.gitlab.commit_sha=$CI_BUILD_REF /d:sonar.gitlab.ref_name=$CI_BUILD_REF_NAME /d:sonar.analysis.mode="preview"

Since TeamCity does not have access to the GitLab parameters in this command we need to find them in a different way:

#### $CI\_PROJECT\_ID ####

This is an integer ID which uniquely identifies the project in GitLab.

To find out its value you can either go to Project -> Settings -> General settings in GitLab or we can query the GitLab API ourselves. You can navigate to ```http://$YOUR-GITLAB-URL/api/v4/projects``` to display the full list of projects. To view a project by ID and ensure you have the correct project you can view by id at ```http://$YOUR-GITLAB-URL/api/v4/projects/$ID```.

#### $CI\_BUILD\_REF ####

The full SHA of the currently building commit. In PowerShell on TeamCity this can be accessed as follows ```$commit = "%build.vcs.number%"```.

#### $CI\_BUILD\_REF\_NAME

The branch name. From PowerShell on TeamCity this can be accessed by: ```$branch = "%teamcity.build.branch%"```. To remove the full qualification you can use ```$branch = $branch.Replace("refs/heads/", "")```.

Now to start Sonar analysis on TeamCity in a PowerShell step assuming the Sonar MSBuild scanner is in ```C:\sonar\bin``` and your project ID is 3 you can run:

    $commit = "%build.vcs.number%"
    $branch = "%teamcity.build.branch%"
    $branch = $branch.Replace("refs/heads/", "")

    C:\sonar\bin\SonarQube.Scanner.MSBuild.exe begin /k:"KEY" /n:"PROJECT NAME" /v:"0.0.1" /d:sonar.gitlab.project_id=3 /d:sonar.gitlab.commit_sha=$commit /d:sonar.gitlab.ref_name=$branch /d:sonar.analysis.mode="preview" /d:sonar.scm.provider=git```

Replace the project key, name, version and id values with your values.

It's important to set the analysis mode to **preview** otherwise the sonar-gitlab plugin will not work.

With the analysis started run your build as normal.

For completing the analysis no GitLab specific changes are required:

    C:\sonar\bin\SonarQube.Scanner.MSBuild.exe end

This will then perform the Sonar Scanner analysis, send the results to the SonarQube server which will then forward them to TeamCity.

Hopefully this helps you get TeamCity talking to SonarQube talking to GitLab. Let me know what static analysis tools you use for your C# projects in the comments.

[link1]: https://www.sonarqube.org/ "SonarQube website"
