# Coding Influencer Assistant

Enter into the chat what kind of article should be written.  
To ensure that it is grounded on real public and also your internal data, please provide additional context.

## Sample

> Current limitation: Needs to be strict this format.

``` yaml
researchContext: Can you find information about user 'ormikopo1988' from GitHub and try to understand the coding language preferences he has?  
internalKnowledgeContext: Return best coding and code review practices based on the language of the user?  
writingAsk: Based on the research and internal knowledge information gathered, write an engaging article around best coding practices to follow for users with similar coding language preferences.
```

## Research Agent

*Which user's GitHub information should I fetch?*

This agent uses your context / question, and formulates out of that GitHub queries.  
After that the findings are summarized.

## Internal Knowledge Agent

*For which coding language should I look for best practices inside the internal knowledge store?*

This agent uses your context / question, formulates out of that up to 5 specialized queries and uses a internal Vector store to find matching best practices around coding & code reviews with semantic search.  
After that the findings are summarized.

## Writer Agent

*What kind of writing should I do?*

This agent uses your instructions, the other contexts and the research and internal knowledge results to produce an article.  
If it gets feedback for rework, it will do so.

## Editor Agent

Reviews the outcomes from the writer agent and provides feedback to it.  
This agent also decides when the article is accepted and no further rework necessary.
